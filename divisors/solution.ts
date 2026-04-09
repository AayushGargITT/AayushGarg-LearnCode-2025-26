export function countDivisors(n: number): number {
    if (n <= 0) {
        throw new Error("n must be positive");
    }

    let count = 0;

    for (let i = 1; i * i <= n; i++) {
        if (n % i === 0) {
            count++;

            if (i !== n / i) {
                count++;
            }
        }
    }

    return count;
}


export function hasSameDivisorCount(a: number, b: number): boolean {
    return countDivisors(a) === countDivisors(b);
}


export function countValidNumbers(k: number): number {
    if (k <= 2) return 0;

    let result = 0;

    for (let n = 2; n < k; n++) {
        if (hasSameDivisorCount(n, n + 1)) {
            result++;
        }
    }

    return result;
}