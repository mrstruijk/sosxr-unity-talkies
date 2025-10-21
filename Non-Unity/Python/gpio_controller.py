from machine import Pin
import sys
import utime

class GPIOController:
    def __init__(self, feedback_pin=25):
        self.pins = {}
        self.feedback_pin = feedback_pin
        print("Note: only the first connected terminal receives flushed messages.")

    def get_pin(self, pin_num: int) -> Pin:
        return self.pins.setdefault(pin_num, Pin(pin_num, Pin.OUT))

    def blink_feedback(self, times=3, duration_ms=50):
        """Briefly flash the feedback LED without permanently changing its state"""
        if self.feedback_pin not in self.pins:
            # Feedback pin not yet controlled, skip flashing
            return
        
        pin = self.pins[self.feedback_pin]
        original_state = pin.value()
        
        for _ in range(times):
            pin.on()
            utime.sleep_ms(duration_ms)
            pin.off()
            utime.sleep_ms(duration_ms)
        
        pin.value(original_state) # Restore original state

    def send_line(self, line: str):
        sys.stdout.write(line + "\n")
        # self.blink_feedback(3, 30)  # 3 quick blinks, 30ms each

    def handle_command(self, line: str):
        parts = line.strip().split(',')
        if not parts or not parts[0]:
            return

        cmd = parts[0].upper()

        try:
            if cmd == "SET" and len(parts) >= 3:
                pin_num, value = map(int, parts[1:3])
                self.get_pin(pin_num).value(value)
                self.send_line(f"OK,SET,{pin_num},{value}")

            elif cmd == "GET" and len(parts) >= 2:
                pin_num = int(parts[1])
                value = self.get_pin(pin_num).value()
                self.send_line(f"OK,GET,{pin_num},{value}")

            elif cmd == "GETALL":
                if not self.pins:
                    self.send_line("OK,NO_PINS_SET")
                else:
                    for pin_num, pin in self.pins.items():
                        self.send_line(f"OK,GET,{pin_num},{pin.value()}")

            elif cmd == "PING":
                self.send_line("OK,PONG,64,I_VALUE_YOU")

            else:
                self.send_line(f"ERR,UNKNOWN_COMMAND,{cmd}")

        except Exception as e:
            self.send_line(f"ERR,EXCEPTION,{type(e).__name__},{e}")

    def run(self):
        buf = ""
        while True:
            c = sys.stdin.read(1)
            if not c:
                continue
            if c == '\n':
                if buf:
                    self.handle_command(buf)
                    buf = ""
            else:
                buf += c

    def cleanup(self):
        for pin in self.pins.values():
            pin.off()
        print("All GPIO pins set low.")


if __name__ == "__main__":
    gpio = GPIOController(feedback_pin=25)
    try:
        gpio.run()
    except KeyboardInterrupt:
        gpio.cleanup()