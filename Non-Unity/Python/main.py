# main.py

from led_controller import LEDController
from gpio_controller import GPIOController

led = LEDController()
gpio = GPIOController()

try:
    print("starting up")
    led.blink_fast() # Show alive
    led.shutdown()
    gpio.run()
except KeyboardInterrupt:
    print("main stopped")