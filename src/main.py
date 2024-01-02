from threading import Thread
import vrchat
import discord
import subprocess, os
import json

def clear():
    os.system('cls' if os.name == 'nt' else 'clear')

global settings
settings = {
    "DiscordID": "",
    "MPD Address": "localhost",
    "MPD Port": 6600,
    "VRchat OSC Address": "localhost",
    "VRchat OSC Port": 9000,
}

def save_settings():
    with open('settings.json', 'w') as f:
        json.dump(settings, f)

def load_settings():
    try:
        with open('settings.json', 'r') as f:
            loaded_settings = json.load(f)
            settings.update(loaded_settings)
        print("Loaded Settings Successfully!")
    except FileNotFoundError:
        print("Error: E001: Could not find 'settings.json': Unable to load settings")

load_settings()

def vrchat_thread():
    vrchat.main(settings['VRchat OSC Address'], settings['VRchat OSC Port'], settings['MPD Address'], settings['MPD Port'])

def discord_thread():
    discord.main(settings["DiscordID"], settings['MPD Address'], settings['MPD Port'])

def main():
    clear()
    choice = 0
    while choice != 6:
        while choice == 5:
            settings_choice = 0
            print("*** Settings ***")
            if (settings["DiscordID"] == ""):
                print("(1) Set Your Discord RPC ID")
            else:
                print(f"(1) Change Your Discord RPC ID - current: {str(settings['DiscordID'])}")
            print(f"(2) Change Your MPD Address - current: {str(settings['MPD Address'])}")
            print(f"(3) Change Your MPD Port - current: {str(settings['MPD Port'])}")
            print(f"(4) Change Your VRchat OSC Address - current: {str(settings['VRchat OSC Address'])}")
            print(f"(5) Change Your VRchat OSC Port - current: {str(settings['VRchat OSC Port'])}")
            print("(6) Main Menu")
            settings_choice = int(input())

            if settings_choice == 1:
                id = input("Enter new Discord RPC ID Here: ")
                settings["DiscordID"] = id
                #Save the discord rpc id
                save_settings()
                print("Saved!")

            if settings_choice == 2:
                addr = input("Enter new MPD Address Here: ")
                settings["MPD Address"] = addr
                save_settings()
                print("Saved!")

            if settings_choice == 3:
                try:
                    port = abs(int(input("Enter new MPD Port Here: ")))
                    settings["MPD Port"] = port
                    save_settings()
                    print("Saved!")
                except ValueError:
                    print("Error: E003: please make sure you enter a number (int) as ports are numbers :3")

            if settings_choice == 4:
                addr = input("Enter new VRchat OSC Address Here: ")
                settings["VRchat OSC Address"] = addr
                save_settings()
                print("Saved!")

            if settings_choice == 5:
                try:
                    port = abs(int(input("Enter new VRchat OSC Port Here: ")))
                    settings["VRchat OSC Port"] = port
                    save_settings()
                    print("Saved!")
                except ValueError:
                    print("Error: E003: please make sure you enter a number (int) as ports are numbers :3")

            if settings_choice == 6:
                save_settings()
                clear()
                choice = 0
        print("*** Discord and VRChat Presence ***")
        if settings["DiscordID"] == "":
            print("** SET A DISCORD ID IN THE SETTINGS!**")
        print("(1) Start Both")
        print("(2) Start VRChat OSC")
        print("(3) Start Discord RPC")
        print("(4) Install Dependencies")
        print("(5) Settings")
        print("(6) Exit")
        choice = int(input())

        if choice == 1:
            print("Starting VRChat OSC, Starting Discord RPC...")
            clear()
            Thread(target = vrchat_thread).start()
            Thread(target = discord_thread).start()

        if choice == 2:
            print("Starting VRChat OSC...")
            clear()
            vrchat_thread()

        if choice == 3:
            print("Starting Discord RPC...")
            clear()
            discord_thread()

        if choice == 4:
            subprocess.run("pip install -r dep.txt")

        if choice == 5:
            clear()

if __name__ == "__main__":
    main()

save_settings()
clear()