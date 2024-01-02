from mpd import MPDClient

def get_music(addr, port):
    output = "Error: E002: this program is real messed up if you are seeing this :3"

    try:
        client = MPDClient()

        client.connect(addr, port)
        client.update()
        
        status = client.status()
        song = client.currentsong()
            
        if status["state"] == "pause":
            output ="⏸️ Paused"
        else:
            if song is not None:
                artist = song.get('artist', 'Unknown Artist')
                title = song.get('title', 'Unknown Title')
                output = f"▶️ {artist} - {title}"
            else:
                output = "No songs in queue"
    except ConnectionError:
        output = "MPD Offline :3"

    return output