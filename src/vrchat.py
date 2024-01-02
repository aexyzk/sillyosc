from pythonosc.udp_client import SimpleUDPClient
from pythonosc import udp_client
import time

import music
import system_stats

osc_message = ["", True]

def send_message():
    osc_message[0] = string_to_forward
    client.send_message("/chatbox/input",osc_message)
    print("Sent messange to VRChat!")

def main(address, port, mpd_address, mpd_port):
    global client
    client = SimpleUDPClient(address, port)

    while True:
        global string_to_forward
        string_to_forward = f"{str(system_stats.get_system_status())} | {str(music.get_music(mpd_address, mpd_port))}"
        send_message()
        time.sleep(2)