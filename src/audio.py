import naoqi
import pyaudio
import keyboard
import numpy as np


"""
File not used in this version of the project.
"""

class Audio:
    def __init__(self, broker, ip, port):
        self.broker = broker
        self.audio = naoqi.ALProxy("ALAudioDevice", ip, port)
        self.tts = naoqi.ALProxy("ALTextToSpeech", ip, port)
        self.tts.setVolume(0.5)
        
        # Subscribe to the audio device with a unique name
        self.client_name = "python_audio_client"
        #self.audio.subscribe(self.client_name)
        
        #self.audio.enableEnergyComputation()

        # Initialize PyAudio
        self.p = pyaudio.PyAudio()
        self.stream = self.p.open(format=pyaudio.paInt16,
                                  channels=1,
                                  rate=16000,
                                  output=True)

    def get_audio(self):
        self.right = self.audio.getRightMicEnergy()
        self.left = self.audio.getLeftMicEnergy()
        return self.right, self.left
    

    def processRemote(self, nbOfChannels, nbOfSamplesByChannel, timeStamp, buffer):
        # Convert audio data to numpy array
        audio_array = np.frombuffer(buffer, dtype=np.int16)
        # Play audio data on PC
        print(audio_array)
        self.stream.write(audio_array.tobytes())


    def play_audio(self):
        while True:
            # Get audio data from Pepper
            audio_data = self.audio.getFrontMicEnergy()
            # Convert audio data to numpy array
            #audio_array = np.frombuffer(bytearray(audio_data), dtype=np.int16)
            # Play audio data on PC
            #self.stream.write(audio_data.tobytes())
            print (audio_data)
            

            if keyboard.is_pressed("q"):
                break

    def close(self):
        self.audio.unsubscribe(self.client_name)
        self.stream.stop_stream()
        self.stream.close()
        self.p.terminate()

    def say(self, text):
        self.tts.say(text)

if __name__ == "__main__":
    LOCAL_IP = "0.0.0.0"
    LOCAL_PORT = 0
    PEPPER_IP = "192.168.0.106"
    PEPPER_PORT = 9559

    broker = naoqi.ALBroker("pythonBroker", LOCAL_IP, LOCAL_PORT, PEPPER_IP, PEPPER_PORT)
    audio = Audio(broker, PEPPER_IP, PEPPER_PORT)

    #audio.say("Hello, I am Pepper. How are you today?")
    
    while True:
        if keyboard.is_pressed("q"):
            break
        
 