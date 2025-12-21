import random


def is_valid_number(number):
    if number.isdigit() and 1 <= int(number) <= 100:
        return True
    else:
        return False


def main():
    number_to_guess = random.randint(1, 100)
    is_guess_correct = False
    user_input = input("Guess a number between 1 and 100:")
    total_guesses_made = 0
    while not is_guess_correct:
        if not is_valid_number(user_input):
            user_input = input(
                "I wont count this one Please enter a number between 1 to 100"
            )
            continue
        else:
            total_guesses_made += 1
            user_input = int(user_input)
        if user_input < number_to_guess:
            user_input = input("Too low. Guess again")
        elif user_input > number_to_guess:
            user_input = input("Too High. Guess again")
        else:
            print("You guessed it in", total_guesses_made, "guesses!")
            is_guess_correct = True


main()
