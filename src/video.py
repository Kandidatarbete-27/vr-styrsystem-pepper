
"""
This script is used to display the video feed from the Pepper robot's camera.
It is working as expected.
"""

import cv2
import naoqi
import numpy as np

class Video:
    def __init__(self, broker, ip, port):
        self.broker = broker
        self.video = naoqi.ALProxy("ALVideoDevice", ip, port)
        self.resolution = 14     # sets resolution, please check the documentation for available options
        self.colorSpace = 11     
        self.fps = 15            #  0 - 15 fps 
        self.cameraID = 3 #3 is eyes, 0 is top camera and 1 is bottom camera, 2 is depth camera
        self.nameID = self.video.subscribeCamera("python_client", self.cameraID, self.resolution, self.colorSpace, self.fps)
        self.image = None

    def get_frame(self):
        self.image = self.video.getImageRemote(self.nameID)
        if self.image is not None:
            width = self.image[0]
            height = self.image[1]
            array = self.image[6]
            image = np.frombuffer(array, dtype=np.uint8).reshape((height, width, 3))
            image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            return image
        return None
    
    def release(self):
        self.video.unsubscribe(self.nameID)

    def display_video(self):
        while True:
            print("Displaying video feed...")
            frame = self.get_frame()
            if frame is not None:
                cv2.imshow("Camera Feed", frame)
            if cv2.waitKey(1) & 0xFF == ord("q"):  # 'q' key to exit
                break
        cv2.destroyAllWindows()

def run_video(broker, ip, port):
    video = Video(broker, ip, port)
    video.display_video()
    video.release()

# Test, if this script is run directly
if __name__ == "__main__":

    LOCAL_IP = "0.0.0.0"
    LOCAL_PORT = 0
    PEPPER_IP = "192.168.0.108"
    PEPPER_PORT = 9559

    broker = naoqi.ALBroker("pythonBroker", LOCAL_IP, LOCAL_PORT, PEPPER_IP, PEPPER_PORT)
    video = Video(broker, PEPPER_IP, PEPPER_PORT)
    video.display_video()
    video.release()
    broker.shutdown()
    exit(0)
