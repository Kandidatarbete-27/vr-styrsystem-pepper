from naoqi import ALProxy, ALModule, ALBroker
import time
import numpy as np

class AudioModule(ALModule):
    def __init__(self, name, ip, port):
        ALModule.__init__(self, name)
        self.name = name
        self.audio_device = ALProxy("ALAudioDevice", ip, port)
        self.audio_device.setClientPreferences(name, 48000, 4, 0)
        self.audio_device.subscribe(name)
        self.data = None
        self.audio_device.enableEnergyComputation()

    def processRemote(self, nbOfChannels, nbOfSamplesByChannel, timeStamp, inputBuffer):
        # Process the audio buffer here
        self.data = inputBuffer
        print("Processing audio buffer...")

    def unsubscribe(self):
        self.audio_device.unsubscribe(self.name)

if __name__ == "__main__":
    PEPPER_IP = "192.168.0.106"
    PEPPER_PORT = 9559
    LOCAL_IP = "0.0.0.0"
    LOCAL_PORT = 0

    # Initialize the broker
    broker = ALBroker("pythonBroker", LOCAL_IP, LOCAL_PORT, PEPPER_IP, PEPPER_PORT)

    # Create and register the AudioModule
    audio_module = AudioModule("python_client", PEPPER_IP, PEPPER_PORT)

    try:
        while True:
            time.sleep(1)
            print("Running...")
            if audio_module.data is not None:
                print("Audio data received")
                # You can process the audio data here
                # For example, you can convert it to a numpy array
                audio_data = np.frombuffer(audio_module.data, dtype=np.int16)
                print(audio_data)
    except KeyboardInterrupt:
        print("Interrupted by user, shutting down")
    finally:
        audio_module.unsubscribe()
        broker.shutdown()