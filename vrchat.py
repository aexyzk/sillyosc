from __future__ import print_function
from pythonosc.udp_client import SimpleUDPClient
import ctypes
import psutil
import time
from datetime import datetime
import GPUtil
import asyncio
from pythonosc import udp_client

client = SimpleUDPClient("127.0.0.1", 9000)

print(f"Started OSC Server: https://github.com/aethefurry/discord-vrchat-presence")
osc_message = ["", True]

def send_message():
    osc_message[0] = string_to_forward
    client.send_message("/chatbox/input",osc_message)
    print(string_to_forward)

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

if __name__ == '__main__':
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

        global string_to_forward
        string_to_forward = ("RAM: " + str(mem_per) + "% | " + "GPU: " + str(load) + "% | " + "CPU: " + str(cpu_per) + "%" + " | " + str(now)+ " | " + str(song))
        send_message()
        time.sleep(2)