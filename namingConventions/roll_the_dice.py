import random
def roll_dice(total_sides):
    dice_value=random.randint(1, total_sides)
    return dice_value


def main():
    total_sides=6
    keep_rolling =True
    while keep_rolling:
        user_choice=input("Ready to roll? Enter Q to Quit")
        if user_choice.lower() !="q":
            roll_result=roll_dice(total_sides)
            print("You have rolled a",roll_result)
        else:
            keep_rolling=False
main()