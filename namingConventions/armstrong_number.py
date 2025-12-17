def calculate_armstrong_sum(number):
    sum_of_powers = 0
    total_digits = 0
    temp_number = number
    while temp_number > 0:
        total_digits = total_digits + 1
        temp_number = temp_number // 10
    temp_number = number
    for n in range(1, temp_number + 1):
        remainder = temp_number % 10
        sum_of_powers = sum_of_powers + (remainder**total_digits)
        temp_number //= 10
    return sum_of_powers

user_input = int(input("\nPlease Enter the Number to Check for Armstrong: "))

if user_input == calculate_armstrong_sum(user_input):
    print("\n %d is Armstrong Number.\n" % user_input)
else:
    print("\n %d is Not a Armstrong Number.\n" % user_input)
