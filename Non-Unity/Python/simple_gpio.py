# simple_gpio.py

import machine
import utime

class SimpleGPIO:
    def __init__(self, pin_number):
        # Initialize the GPIO pin as an output
        print("Running")
        self.pin = machine.Pin(pin_number, machine.Pin.OUT)

    def enable_pin(self):
        self.pin.high()

    def disable_pin(self):
        self.pin.low()



if __name__ == "__main__":
    """Enter a valid pin number below, or "LED" to use the onboard LED."""
    gpio = SimpleGPIO(16) 

    try:
        while True:
            gpio.enable_pin()
            utime.sleep(1)
            
            gpio.disable_pin()
            utime.sleep(1) 
    except KeyboardInterrupt:
        print("Program terminated by user.")
