from threading import Thread
import vrchat
import discord
import subprocess, os
import json

global settings
settings = {
    "DiscordID": "",
    "IsSpotifyFree": True,
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
    vrchat.main(settings["IsSpotifyFree"])

def discord_thread():
    discord.main(settings["IsSpotifyFree"], settings["DiscordID"])

def main():
    os.system('cls')
    choice = 0
    while choice != 6:
        while choice == 5:
            settings_choice = 0
            print("*** Settings ***")
            if (settings["DiscordID"] == ""):
                print("(1) Set Your Discord RPC ID")
            else:
                print("(1) Change Your Discord RPC ID"+" - current: "+str(settings["DiscordID"]))
            print("(2) Is Spotify Free? (detects if media is paused)"+" - "+str(settings["IsSpotifyFree"]))
            print("(3) Main Menu")
            settings_choice = int(input())

            if settings_choice == 1:
                id = input("Enter Discord RPC ID Here: ")
                settings["DiscordID"] = id
                #Save the discord rpc id
                save_settings()
                print("Saved!")

            if settings_choice == 2:
                if settings["IsSpotifyFree"] == True:
                    settings["IsSpotifyFree"] = False
                else:
                    settings["IsSpotifyFree"] = True
                save_settings()
                os.system("cls")

            if settings_choice == 3:
                save_settings()
                os.system("cls")
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
            print("Starting VRChat OSC, Starting Discord RPC...")
            os.system('cls')
            Thread(target = vrchat_thread).start()
            Thread(target = discord_thread).start()

        if choice == 2:
            print("Starting VRChat OSC...")
            os.system('cls')
            vrchat_thread()

        if choice == 3:
            print("Starting Discord RPC...")
            os.system('cls')
            discord_thread()

        if choice == 4:
            subprocess.run("pip install -r dep.txt")

        if choice == 5:
            os.system("cls")

if __name__ == "__main__":
    main()

save_settings()
os.system("cls")