# led_rgb_controller.py

import time
from machine import Pin
import neopixel

class LEDRGBController:
    COLORS = {
        "red": (255, 0, 0),
        "green": (0, 255, 0),
        "blue": (0, 0, 255),
        "yellow": (255, 255, 0),
        "cyan": (0, 255, 255),
        "magenta": (255, 0, 255),
        "white": (255, 255, 255),
        "off": (0, 0, 0)
    }
    
    def __init__(self, pin=16, num_leds=1):
        self.np = neopixel.NeoPixel(Pin(pin), num_leds)
        self.color = (0, 0, 0)

    def _set_color(self, color):
        self.color = color
        self.np[0] = color
        self.np.write()

    def set_color_by_name(self, name):
        name = name.lower()
        if name in self.COLORS:
            self._set_color(self.COLORS[name])
        else:
            raise ValueError(f"Unknown color: {name}")

    def on(self, color=None):
        if isinstance(color, str):
            self.set_color_by_name(color)
        elif isinstance(color, tuple):
            self._set_color(color)
        else:
            self._set_color(self.color)

    def off(self):
        self._set_color(self.COLORS["off"])

    def blink(self, color="red", on_time=0.25, off_time=0.25, times=3):
        for _ in range(times):
            self.on(color)
            time.sleep(on_time)
            self.off()
            time.sleep(off_time)

    def blink_fast(self, color="green", times=6):
        self.blink(color=color, on_time=0.2, off_time=0.1, times=times)


    def blink_slow(self, color="blue", times=4):
        self.blink(color=color, on_time=1, off_time=1, times=times);


    def shutdown(self):
        self.off()


if __name__ == "__main__":
    led = LEDRGBController()
    try:
        led.blink_fast()
        led.blink_fast("yellow")
        led.blink_slow((255,255,255))
        led.blink("cyan", 0.1, 0.05, 10)
    except KeyboardInterrupt:
        led.shutdown()
        print("LED shutdown.")
    

