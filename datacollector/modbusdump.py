import subprocess
import json

# Configuration
SLAVE_IP = '127.0.0.1'
PORT = 502
START_ADDRESS = 1
TOTAL_REGISTERS = 1000  # Total number of registers to read
REGISTER_READ_LIMIT = 125  # Maximum number of registers that can be read in one command
FILENAME = "modbus_data.json"

def run_modpoll(start_address, count):
    """Run modpoll command and return the output."""
    command = [
        "../modpoll/x86_64-linux-gnu/modpoll",
        "-m", "tcp",
        "-a", "1",
        "-r", str(start_address),
        "-c", str(count),
        "-t", "3",  # Holding registers
        "-1",  # Single poll
        f"{SLAVE_IP}"
    ]

    result = subprocess.run(command, capture_output=True, text=True)
    return result.stdout

def parse_modpoll_output(output):
    """Parse modpoll output to extract register values."""
    register_data = {}
    lines = output.split('\n')
    for line in lines:
        if '[' in line:  # Identify lines containing register data
            parts = line.split(':')
            address = int(parts[0].strip('[').strip(']'))
            value = int(parts[1].strip(), 16)  # Convert value from hex to int
            register_data[address] = value
    return register_data

def save_to_file(data, filename):
    """Save data to a JSON file."""
    with open(filename, 'w') as file:
        json.dump(data, file, indent=4)
    print(f"Data saved to {filename}")

def main():
    all_registers = {}
    for start in range(START_ADDRESS, START_ADDRESS + TOTAL_REGISTERS, REGISTER_READ_LIMIT):
        count = min(REGISTER_READ_LIMIT, START_ADDRESS + TOTAL_REGISTERS - start)
        output = run_modpoll(start, count)
        registers = parse_modpoll_output(output)
        all_registers.update(registers)

    save_to_file(all_registers, FILENAME)

if __name__ == "__main__":
    main()

