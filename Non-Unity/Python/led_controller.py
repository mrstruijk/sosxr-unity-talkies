# led_controller.py

from machine import Pin
import time

class LEDController:
    def __init__(self, pin_name="LED"):
        self.pin = pin_name
        self.led = Pin(pin_name, Pin.OUT)

    def initialize(self):
        self.on()

    def on(self):
        self.led.on()

    def off(self):
        self.led.off()

    def blink(self, on_time=0.25, off_time=0.25, times=3):
        for _ in range(times):
            self.on()
            time.sleep(on_time)
            self.off()
            time.sleep(off_time)

    def blink_fast(self, times=6):
        self.blink(on_time=0.2, off_time=0.1, times=times)

    def blink_slow(self, times=6):
        self.blink(on_time=1, off_time=1, times=times)

    def shutdown(self):
        self.off()
        self.led = Pin(self.pin, Pin.IN)


if __name__ == "__main__":
    led = LEDController()
    try:
        led.blink_fast()
        led.blink_slow()
        led.blink(on_time=0.1, off_time=0.05, times=10)
    except KeyboardInterrupt:
        led.shutdown()
        print("LED shutdown.")
