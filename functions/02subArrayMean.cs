
using System;
class MyClass {
  static int[] TakeInputFromConsole() {
    return Array.ConvertAll(Console.ReadLine().Split(' '), int.Parse);
  }

  static void ConsoleOutput(long output) {
    Console.WriteLine(output);
  }
  static long[] CalculatePrefixSum(int[] arr, int totalIntegers) {
    long[] prefixSum = new long[totalIntegers + 1];
    prefixSum[0] = 0;
    for (int i = 1; i <= totalIntegers; i++) {
      prefixSum[i] = prefixSum[i - 1] + arr[i - 1];
    }
    return prefixSum;
  }
  static void CalculateMeanAndOutput(long[] arr, int totalQueries) {
    for (var i = 0; i < totalQueries; i++) {
      var queryInput = TakeInputFromConsole();
      int left = queryInput[0];
      int right = queryInput[1];
      ConsoleOutput(
          (long)((long)(arr[right] - arr[left - 1]) / (right - left + 1)));
    }
  }
  static void solve() {
    var input = TakeInputFromConsole();
    int totalIntegers = input[0];
    int totalQueries = input[1];
    var arr = TakeInputFromConsole();
    long[] prefixSum = CalculatePrefixSum(arr, totalIntegers);

    CalculateMeanAndOutput(prefixSum, totalQueries);
  }
  static void Main(string[] args) {
    solve();
  }
}