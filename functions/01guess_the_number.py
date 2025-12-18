import random
MIN=1
MAX=100

def is_valid_number(number):
    if number.isdigit() and 1 <= int(number) <= 100:
        return True
    else:
        return False

def get_random_number_in_range():
    return random.randint(MIN,MAX)

def is_too_low(guess, target):
    return guess < target

def is_too_high(guess, target):
    return guess > target

def take_input():
    return input(f"Guess a number between {MIN} and {MAX}: ")

def notify_too_low():
    print("Too low. Guess again.")

def notify_too_high():
    print("Too high. Guess again.")

def notify_invalid_input():
    print(f"I wont count this one. Please enter a number between {MIN} and {MAX}.")

def print_success(total_guesses):
    print(f"You guessed it in {total_guesses} guesses!")

def get_valid_guess():
    while True:
        user_input = take_input()
        if is_valid_number(user_input):
            return int(user_input)
        notify_invalid_input()


def play_game(number_to_guess):
    total_guesses_made = 0
    is_guess_correct=False
    while not is_guess_correct:
        guess=get_valid_guess()
        total_guesses_made += 1

        if is_too_low(guess, number_to_guess):
            notify_too_low()
        elif is_too_high(guess, number_to_guess):
            notify_too_high()
        else:
            return total_guesses_made


def main():
    number_to_guess =get_random_number_in_range()
    total_guesses = play_game(number_to_guess)
    print_success(total_guesses)

main()