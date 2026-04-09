import { countDivisors, hasSameDivisorCount, countValidNumbers } from "./solution";

describe("countDivisors", () => {
    test("should count divisors correctly", () => {
        expect(countDivisors(1)).toBe(1);
        expect(countDivisors(2)).toBe(2);
        expect(countDivisors(14)).toBe(4);
    });

    test("should throw error for invalid input", () => {
        expect(() => countDivisors(0)).toThrow();
        expect(() => countDivisors(-5)).toThrow();
    });
});

describe("hasSameDivisorCount", () => {
    test("should return true when divisor counts match", () => {
        expect(hasSameDivisorCount(2, 3)).toBe(true);
    });

    test("should return false when divisor counts differ", () => {
        expect(hasSameDivisorCount(3, 4)).toBe(false);
    });
});

describe("countValidNumbers", () => {
    test("should match given example", () => {
        expect(countValidNumbers(15)).toBe(2);
    });

    test("should handle small values", () => {
        expect(countValidNumbers(3)).toBe(1);
        expect(countValidNumbers(2)).toBe(0);
    });

    test("should handle edge cases", () => {
        expect(countValidNumbers(0)).toBe(0);
        expect(countValidNumbers(-10)).toBe(0);
    });
});