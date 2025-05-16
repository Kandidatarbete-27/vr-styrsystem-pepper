import qi
import time
import numpy as np
import threading
import socket
import time

class PepperAudioFeed():
    def __init__(self, app):
        self.app = app
        self.app.start()
        self.session = self.app.session

        self.audio_service = self.session.service("ALAudioDevice")
        self.isProcessingDone = False
        self.module_name = "PepperAudioFeed"
        self.isUpdated = 0
        self.fs = 16000  # Sample rate (Hz)
        self.running = True  # Flag to control thread execution
        self.thread1 = None
        self.thread2 = None
        self.isRegistered = False

    def startProcessing(self):
        """Subscribe to microphone and process live audio."""
        self.audio_service.setClientPreferences(self.module_name, 16000, 1, 0)
        self.audio_service.subscribe(self.module_name)

    def processRemote(self, nbOfChannels, samplesPerChannel, timestamp, inputBuffer):
        # NaoQI requires all four of the inputs, even though we don't use three of them, do not remove any.
        self.micFront = self.convertStr2SignedInt(inputBuffer)
        self.isUpdated = 1

    def convertStr2SignedInt(self, data):
        """Convert byte data to float32 (-1 to 1)."""
        return np.frombuffer(data, dtype=np.int16).astype(np.float32) / 32768.0

    def startAudioThread(self):
        if not self.isRegistered:
            self.app.session.registerService("PepperAudioFeed", self)
            self.isRegistered = True
        self.startProcessing()
        while self.running:
            time.sleep(0.1)
        del self.micFront
        self.audio_service.unsubscribe(self.module_name)

    def startSenderThread(self, target_ip, target_port):
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        while not hasattr(self, "micFront"):
            time.sleep(0.1)  # Small delay to prevent excessive CPU usage
        np.set_printoptions(threshold=np.inf)
        while self.running:
            while self.isUpdated == 0:
                None
            encoded_data = (np.array2string(self.micFront))
            self.isUpdated = 0
            client_socket.sendto(encoded_data.encode(
                "utf-8"), (target_ip, target_port))

def getApp(pepper_ip, pepper_port):
    connection_url = "tcp://" + pepper_ip + ":" + str(pepper_port)
    app = qi.Application(
            ["PepperAudioFeed", "--qi-url=" + connection_url])
    return app

def startAudioFeed(audioFeed, target_ip, target_port):
    audioFeed.thread1 = threading.Thread(target=audioFeed.startAudioThread, args=())
    audioFeed.thread2 = threading.Thread(target=audioFeed.startSenderThread, args=(
        target_ip, target_port,))
    audioFeed.thread1.start()
    audioFeed.thread2.start()
    audioFeed.running = True

def stopAudioFeed(audioFeed):
    audioFeed.running = False
    audioFeed.thread1.join()
    audioFeed.thread2.join()


if __name__ == "__main__":
    app = getApp("192.168.0.108", 9559)
    MyAudioFeed = PepperAudioFeed(app)
    for i in range(10):
        startAudioFeed(MyAudioFeed, "192.168.0.103", 11000)
        time.sleep(3)
        stopAudioFeed(MyAudioFeed)
        time.sleep(3)