# RealtimeService

## Overview
RealtimeService is a C# Console Application designed to fetch network usage data (wifi usage logs) from a specified IP server. The application reads data via TCPClient, processes and manipulates it, structures it into a DataTable, and then:

1. Sends the processed data to Power BI for visualization in a dashboard using Power BI API.
2. Inserts the data into a PostgreSQL database for logging and storage.

## Features

- **Network Usage Data Collection**: Collects wifi usage data of employees from a specified IP server.
- **Data Processing**: The raw data is processed, modified, and structured based on predefined conditions.
- **Efficient Data Handling**: Processes large datasets (up to 5000 records at a time).
- **Power BI Integration**: Sends the processed data to Power BI for real-time dashboard updates.
- **PostgreSQL Logging**: Saves the data into a PostgreSQL table for historical logging.
- **Data Manipulation**: Applies transformations like converting to uppercase, replacing certain characters, and filtering records based on conditions.

## Requirements

- .NET Framework (or .NET Core) compatible environment
- PostgreSQL database
- Power BI API credentials and access
- TCP/IP server to provide network usage logs

## Setup and Installation

1. Clone the repository to your local machine:
   ```bash
   git clone https://github.com/your-username/RealtimeService.git
2. Open the solution in Visual Studio or your preferred IDE.

3. Install required NuGet packages:
- Power BI API client libraries
- Npgsql (for PostgreSQL integration)

4. Set up your PostgreSQL database and configure connection strings in the appsettings.json or directly in the code.

5. Configure Power BI API credentials (client ID, secret, etc.) to authenticate the application for data submission.

## Usage
1. Run the Console Application: Execute the console application to start processing data from the IP server.

2. Data Flow:
- Data is fetched from the IP server using a TCPClient.
- Every second, 5000 records are read and processed.
- The processed data is inserted into a DataTable.
- Based on predefined conditions, the DataTable is manipulated and formatted.
- The DataTable is sent to Power BI via API for real-time dashboard updates.
- The same DataTable is inserted into PostgreSQL for logging purposes.

3. View Data in Power BI: The Power BI dashboard will update in real-time based on the data sent from the application.

4. Access Data in PostgreSQL: View the logged data in the configured PostgreSQL table for historical analysis.

## Code Explanation

### Data Collection:
- The application uses a TcpClient to connect to the specified IP server and reads the data stream.

### Data Processing:
- The incoming data is manipulated based on certain conditions (uppercase conversion, character replacements, etc.).
- Large datasets (5000 records) are processed efficiently to avoid memory overload.

### Data Insertion:
- The processed data is inserted into a DataTable with predefined columns.
- The DataTable is converted to a string and sent to Power BI using its API.
- Simultaneously, the same DataTable is used to insert the data into a PostgreSQL database.

## License
This project is licensed under the MIT License - see the LICENSE file for details.
