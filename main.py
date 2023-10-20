from threading import Thread
import vrchat
import discord
import subprocess

def vrchat_thread():
    vrchat.main()

def discord_thread():
    discord.main()

def main():
    global should_exit
    choice = 0
    while choice != 6:
        while choice == 5:
            settings_choice = 0
            print("*** Settings ***")
            print("(1) Settings Option 1")
            print("(2) Main Menu")
            settings_choice = int(input())

            if settings_choice == 2:
                choice = 0
        print("*** Discord and VRChat Presence ***")
        print("(1) Start Both")
        print("(2) Start VRChat OSC")
        print("(3) Start Discord RPC")
        print("(4) Install Dependencies")
        print("(5) Settings")
        print("(6) Exit")
        choice = int(input())

        if choice == 1:
            should_exit = False
            print("Starting both...")
            Thread(target = vrchat_thread).start()
            Thread(target = discord_thread).start()

        if choice == 2:
            should_exit = False
            print("Starting VRChat OSC...")
            Thread(target = vrchat_thread).start()

        if choice == 3:
            should_exit = False
            print("Starting Discord RPC...")
            Thread(target = discord_thread).start()

        if choice == 4:
            subprocess.run("pip install -r dep.txt")

if __name__ == "__main__":
    main()