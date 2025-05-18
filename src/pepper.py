
"""
This is main progam, that runs all modules
Test each module separately before running them here
"""

import naoqi as nq
import keyboard
import sys
import multiprocessing as mp
import time
import threading


#local imports from selfmade modules
import config as cfg           
import motions as M
import video as V
import com
import pepperaudiofeed as A

#GLOBAL VARIABLES
broker = None # broker init for program to connect to pepper

AUDIO = True #set to true if you want to receive audio feed

def init_connection():
    global broker
    # Initialize the broker
    try:
        broker = nq.ALBroker("pythonBroker", cfg.LOCAL_IP, cfg.LOCAL_PORT, cfg.PEPPER_IP, cfg.PEPPER_PORT)
        print("broker connected to Pepper")
    except RuntimeError as e:
        print("Could not connect to the robot. Please check the IP address and port.")
        print(e)
        sys.exit(1)
    
def run_video(queue):
    global video_handler, broker, video_com_handler
    try:
        video_handler = V.Video(broker, cfg.PEPPER_IP, cfg.PEPPER_PORT)
        print("Video proxy created")
    except Exception as e:
        print("Proxy to Video could not be created")
        print(e)
        sys.exit(1)
    
    while True:
        try: 
            video_com_handler = com.ComSender(cfg.SERVER_IP, cfg.PORT_VIDEO)
            print("Connected to server for video feed transmission")
            break
        except Exception as e:
            print("Could not connect to videolink")
            print(e)
            time.sleep(1)
            
    while True:
        frame = video_handler.get_frame()
        if frame is not None and frame.size > 0:
            try:
                video_com_handler.send_frame(frame)
            except Exception as e:
                print("Error sending frame:", e)
        else:
            print("Received an empty frame")   

        if queue.qsize() > 0:
            data = queue.get()
            if data == "stop":
                break

    video_handler.release()


def main():
    global broker, motion_com
    MyAudioFeed = None
    video_queue = mp.Queue()
    video_process = mp.Process(target=run_video, args=(video_queue,))
    video_process.start()
    print("video process started")
    if AUDIO:
        audioApp = A.getApp(cfg.PEPPER_IP, cfg.PEPPER_PORT)
        MyAudioFeed = A.PepperAudioFeed(audioApp)
        A.startAudioFeed(MyAudioFeed, cfg.SERVER_IP, cfg.PORT_ADUIO)
        print("Audio feed started")

    while True:
        try:
            motion_com = com.ComReceiver(cfg.LOCAL_IP, cfg.PORT_INSTRUCTIONS)
            print("Connected to server for motion data reception")
            break
        except Exception as e:
            print("Could not connect to motion data link")
            print(e)
            time.sleep(1)

    motion_handler = M.Motion(broker, cfg.PEPPER_IP, cfg.PEPPER_PORT)

    def quit():
        #end all processes
        print("Stopping")
        video_queue.put("stop", block=False) #end video process
        if AUDIO:
            A.stopAudioFeed(MyAudioFeed) #end audio process
        motion_handler.shutdown() #end motion process
        print("stoped")
        sys.exit(0)    

    def check_if_esc_pressed():
        while True:
            if keyboard.is_pressed("esc"):
                quit()
                break

    esc_thread = threading.Thread(target=check_if_esc_pressed)
    esc_thread.start()

    print("starting loop in pepper.py")  
    while True:   
        # get data from vr
        data = motion_com.receive_json_3()
        if data["type"] == "quit" :
            quit()
            break
        else:
            motion_handler.set_motion_data(data)

    video_process.join()
    video_queue.close()
    video_queue.join_thread()


def shutdown():
    global broker
    broker.shutdown()


if __name__ == "__main__":
    init_connection()
    main()  
    shutdown()
    exit(0)







