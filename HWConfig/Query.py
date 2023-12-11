from flask import Flask, jsonify, request
import mysql.connector
# or import psycopg2 for PostgreSQL

app = Flask(__name__)

# Database configuration
db_config = {
    'user': 'username',
    'password': '**********',
    'host': 'localhost',
    'database': 'dabasename',
    'raise_on_warnings': True
}

# Function to create a database connection
def get_db_connection():
    conn = mysql.connector.connect(**db_config)
    # or conn = psycopg2.connect(**db_config) for PostgreSQL
    return conn

# Route to get device information by ID
@app.route('/device/<int:device_id>', methods=['GET'])
def get_device(device_id):
    conn = get_db_connection()
    cursor = conn.cursor(dictionary=True)

    query = """
    SELECT 
        d.DeviceID, 
        d.Model, 
        d.ManufactureDate, 
        d.WarrantyPeriod, 
        d.Description AS DeviceDescription
    FROM 
        Devices d
    WHERE 
        d.DeviceID = %s
    """

    cursor.execute(query, (device_id,))
    result = cursor.fetchone()

    cursor.close()
    conn.close()

    if result:
        return jsonify(result)
    else:
        return jsonify({"error": "Device not found"}), 404

if __name__ == '__main__':
    app.run(debug=True)
