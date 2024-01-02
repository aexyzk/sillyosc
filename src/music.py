from mpd import MPDClient

def get_music(addr, port):
    output = "Error: E002: this program is real messed up if you are seeing this :3"

    try:
        client = MPDClient()

        client.connect(addr, port)
        print(addr, port)
        client.update()

        status = client.status()
        song = client.currentsong()
        
        if status["state"] == "pause":
            output ="⏸️ Paused"
        else:
            output = f"▶️ {song['artist']} - {song['title']}"
    except ConnectionError:
        output = "MPD Offline :3"

    return output