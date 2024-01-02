from pypresence import Presence
import time
import system_stats
import music

def main(id, addr, port):
    RPC = Presence(id, pipe=0)
    RPC.connect()

    while True:
        RPC.update(details=str(system_stats.get_system_status()), state=str(music.get_music(addr, port)))  # Set the presence
        print("Sent a message to Discord!")
        time.sleep(15)
