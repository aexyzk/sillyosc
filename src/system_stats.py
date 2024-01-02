import ctypes
import psutil
from datetime import datetime
import GPUtil

def get_system_status():
    now = datetime.now().strftime("%H:%M")
    cpu_per = round(psutil.cpu_percent(), 1)
    mem = psutil.virtual_memory()
    mem_per = round(psutil.virtual_memory().percent, 1)
    GPUs = GPUtil.getGPUs()
    load = round(GPUs[0].load * 100, 1)
                            
    return str("RAM: " + str(mem_per) + "% | " + "GPU: " + str(load) + "% | " + "CPU: " + str(cpu_per) + "%" + " | " + str(now))