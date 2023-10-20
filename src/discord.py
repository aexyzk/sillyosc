from __future__ import print_function
import ctypes
import psutil
from pypresence import Presence
import time
from datetime import datetime
import GPUtil
import asyncio

global isPlaying
isPlaying = False

from winsdk.windows.media.control import \
    GlobalSystemMediaTransportControlsSessionManager as MediaManager
from winsdk.windows.media.control import \
    GlobalSystemMediaTransportControlsSessionPlaybackStatus  as PlaybackStatus

async def get_media_info():
    sessions = await MediaManager.request_async()

    current_session = sessions.get_current_session()
    if current_session:
        info = await current_session.try_get_media_properties_async()
        info_dict = {song_attr: info.__getattribute__(song_attr) for song_attr in dir(info) if song_attr[0] != '_'}
        info_dict['genres'] = list(info_dict['genres'])

        return info_dict
    else:
        pass

def get_titles(): 
    EnumWindows = ctypes.windll.user32.EnumWindows
    EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.POINTER(ctypes.c_int), ctypes.POINTER(ctypes.c_int))
    GetWindowText = ctypes.windll.user32.GetWindowTextW
    GetWindowTextLength = ctypes.windll.user32.GetWindowTextLengthW
    IsWindowVisible = ctypes.windll.user32.IsWindowVisible
    
    global titles
    titles = []
    def foreach_window(hwnd, lParam):
        if IsWindowVisible(hwnd):
            length = GetWindowTextLength(hwnd)
            buff = ctypes.create_unicode_buffer(length + 1)
            GetWindowText(hwnd, buff, length + 1) 
            titles.append(buff.value)
        return True
    EnumWindows(EnumWindowsProc(foreach_window), 0)
    return titles
    
def main():
    client_id = '1164610063269384252'
    #https://discord.com/developers or create one with this link

    RPC = Presence(client_id, pipe=0)
    RPC.connect()

    while True:
        current_media_info = asyncio.run(get_media_info())

        now = datetime.now().strftime("%H:%M")
        cpu_per = round(psutil.cpu_percent(), 1)
        mem = psutil.virtual_memory()
        mem_per = round(psutil.virtual_memory().percent, 1)
        GPUs = GPUtil.getGPUs()
        load = round(GPUs[0].load * 100, 1)

        titles = get_titles()

        title_combo = ""
        for title in titles:
            title_combo = title_combo + title
            
        if "Spotify Free" in title_combo:
            isPlaying = False
        if "Spotify Free" not in title_combo:
            isPlaying = True

        title_combo = ""

        if isPlaying == False:
            song="⏸️ Paused"
        elif isPlaying == True:
            song = "▶ "+current_media_info.get("title") + " - " + current_media_info.get("artist")
                            
        RPC.update(details="RAM: " + str(mem_per) + "% | " + "GPU: " + str(load) + "% | " + "CPU: " + str(cpu_per) + "%" + " | " + str(now), state=str(song))  # Set the presence
        print("Sent a message to Discord!")
        time.sleep(15)

if __name__ == '__main__':
    main()