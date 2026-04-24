# P2 – IoT PM2.5 Detection (SOA Project 2)

## Author

**Dušan Sotirov**  
Faculty of Electronic Engineering – Niš  
Service-Oriented Architecture Project

---

## Project Overview

This project is the second phase of an IoT monitoring system built with a Service-Oriented Architecture.

It extends the basic data collection flow from Project 1 (https://github.com/Dulle99/P1-IoT-Smoke-Detection-SOA) by adding **event-driven analytics**, **stream processing**, **time-series storage**, and **cross-service notification**.

The system receives environmental sensor readings, stores the raw readings in MongoDB, publishes selected values to MQTT, processes them through **eKuiper**, detects significant **PM2.5 events**, stores detected events in **InfluxDB**, and then sends a notification through **gRPC** to a separate notification microservice.

---

## Problem Description

The goal of this project is to detect important environmental events in near real time.

In this implementation, the domain event is based on **PM2.5 concentration**.

### What is PM2.5?

**PM2.5** stands for **particulate matter with a diameter of 2.5 micrometers or less**.

These particles are extremely small and can stay suspended in the air for a long time. Because of their size, they can enter deep into the respiratory system and may represent a health risk when their concentration is elevated.

That makes PM2.5 a good candidate for an IoT event-detection scenario:
- it is numeric,
- it can be continuously monitored,
- it can be evaluated against a threshold,
- and it can trigger downstream actions and notifications.

In this project:
- raw sensor-like readings are accepted through the API Gateway,
- the **PM2.5** value is forwarded to MQTT,
- **eKuiper** filters the stream,
- significant PM2.5 events are written to **InfluxDB**,
- and a notification is published for other services in the architecture.

---

## Architecture

```text
Client
  |
  v
API Gateway (.NET)
  | \
  |  \--> DataService (Node.js + MongoDB)  -> stores raw readings
  |
  ---> Mosquitto MQTT Broker
          |
          v
      eKuiper
          |
          v
AnalyticsService (.NET)
          |
          +--> InfluxDB (stores detected PM2.5 events)
          |
          +--> gRPC
                 |
                 v
          NotificationService (Node.js)
                 |
                 v
          MQTT notifications topic
```

---

## Data Flow

### 1. Raw reading submission
A client sends a reading to the Gateway endpoint:

`POST /api/ReadingsMqtt/{deviceId}`

The payload contains the raw reading fields used in Project 1, including:
- `utc`
- `temperatureC`
- `humidityPercent`
- `eCo2Ppm`
- `tVocPpb`
- `pressureHpa`
- `pm25`
- `fireAlarm`

### 2. Raw reading persistence
The Gateway forwards the reading to the DataService, which stores it in **MongoDB**.

### 3. MQTT publication
If saving to the database succeeds, the Gateway converts the reading into a broker message and publishes it to:

`iot/pm25/readings`

Published message shape:

```json
{
  "deviceId": "sensor-1",
  "temperature": 27.5,
  "pm25": 77,
  "timestampUtc": "2024-04-24T20:00:00.0000000Z"
}
```

### 4. Stream processing in eKuiper
eKuiper subscribes to `iot/pm25/readings`, evaluates the incoming stream, and publishes only the relevant PM2.5 events to:

`iot/pm25/events`

Current rule logic:
- stream name: `readings`
- event rule: `pm25 >= 50`

### 5. Analytics processing
The Analytics service subscribes to `iot/pm25/events`, deserializes the event, and stores it in **InfluxDB**.

Stored measurement:
- `pm25_events`

Stored fields:
- `pm25`
- `temperature`

Stored tag:
- `device_id`

### 6. Notification
After the event is successfully stored, Analytics sends a **gRPC** message to the Notification service.

The Notification service then publishes a notification to:

`iot/pm25/notifications`

Example notification message:

```json
{
  "type": "PM25_ALERT",
  "deviceId": "sensor-1",
  "pm25": 77,
  "temperature": 27.5,
  "timestampUtc": "2024-04-24T20:00:00.0000000Z",
  "message": "PM25 alert from device sensor-1 detected PM25 level 77 at 2024-04-24T20:00:00.000Z"
}
```

---

## Implemented Microservices

### 1. AnalyticsService
Technology: **.NET**

Responsibilities:
- subscribes to `iot/pm25/events`
- receives processed MQTT events
- stores detected PM2.5 events in InfluxDB
- sends gRPC notifications to NotificationService

### 2. NotificationService
Technology: **Node.js**

Responsibilities:
- exposes a gRPC endpoint
- receives PM2.5 alert events from AnalyticsService
- publishes PM2.5 notifications to MQTT for other services

### Supporting Services
- **GatewayService (.NET)** – forwards readings to the data layer and publishes MQTT readings
- **DataService (Node.js)** – stores raw readings in MongoDB
- **Mosquitto** – MQTT broker
- **eKuiper** – stream processing engine
- **InfluxDB** – time-series database for detected PM2.5 events
- **MongoDB** – raw reading storage

---

## Technologies Used

- **ASP.NET Core / .NET**
- **Node.js**
- **MongoDB**
- **InfluxDB 2.x**
- **Mosquitto MQTT**
- **eKuiper**
- **gRPC**
- **Docker / Docker Compose**
- **C#**
- **JavaScript**

---

## Project Structure

```text
.
├── docker-compose.yaml
├── infra
│   ├── mosquitto
│   └── ekuiper
│       ├── mqtt_source.yaml
│       ├── pm25-ruleset.json
│       └── init-rules.sh
└── src
    ├── GatewayService
    ├── DataService
    ├── AnalyticsService
    └── NotificationService
```

---

## Ports

| Service | Port |
|---|---:|
| GatewayService | 5000 |
| AnalyticsService | 5001 |
| NotificationService | 5002 |
| DataService | 5003 |
| InfluxDB UI / API | 8086 |
| eKuiper REST API | 9081 |
| Mosquitto MQTT | 1883 |
| MongoDB | 27017 |

---

## MQTT Topics

| Topic | Description |
|---|---|
| `iot/pm25/readings` | raw readings published by Gateway |
| `iot/pm25/events` | filtered PM2.5 events published by eKuiper |
| `iot/pm25/notifications` | notifications published by NotificationService |

---

## InfluxDB Setup

Organization:
- `iot-org`

Bucket:
- `PM25-bucket`

Measurement:
- `pm25_events`

Fields:
- `pm25`
- `temperature`

Tag:
- `device_id`

---

## How eKuiper Is Initialized

This project includes automatic eKuiper setup.

Files:
- `infra/ekuiper/pm25-ruleset.json`
- `infra/ekuiper/init-rules.sh`

Behavior:
- `ekuiper-init` waits for the eKuiper REST API
- then it imports the PM2.5 ruleset automatically

### Important Note
`ekuiper-init` is a **one-shot helper container**.  
After it finishes importing the rules, it stops.  
That is **expected behavior** and does **not** mean eKuiper failed.

You can verify the loaded rules with:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:9081/rules"
```

---

## Prerequisites

Before running the project, make sure you have:

- **Docker Desktop**
- **Git**
- optional: **.NET SDK** if you want to run .NET services locally
- optional: **Node.js** if you want to run Node services locally

---

## How to Run the Project

### 1. Clone the repository

```bash
git clone https://github.com/Dulle99/P2-IoT-Smoke-Detection-SOA.git
cd P2-IoT-Smoke-Detection-SOA
```

### 2. Start the system

```bash
docker compose up --build
```

If you want a fully clean restart:

```bash
docker compose down
docker compose up --build
```

---

## Important Notes About Clean Restarts

eKuiper uses local bind-mounted folders:

- `infra/ekuiper/data`
- `infra/ekuiper/log`

If you want to completely remove old persisted rule state, clear those folders before starting again.

For example in PowerShell:

```powershell
Remove-Item -Recurse -Force .\infra\ekuiper\data\*
Remove-Item -Recurse -Force .\infra\ekuiper\log\*
```

Then start again:

```powershell
docker compose up --build
```

---

## Available APIs

### GatewayService
Base URL:
- `http://localhost:5000`

Useful endpoints:
- `POST /api/ReadingsMqtt/{deviceId}` – store reading and publish PM2.5 reading to MQTT
- `GET /api/readings?limit=...`
- `GET /api/readings/{id}`
- `PUT /api/readings/{id}`
- `DELETE /api/readings/{id}`

Swagger UI:
- `http://localhost:5000/swagger`

### DataService
Base URL:
- `http://localhost:5003`

Swagger / docs:
- `http://localhost:5003/api-docs`
- `http://localhost:5003/api-docs.json`

### eKuiper
REST API:
- `http://localhost:9081`

### InfluxDB
UI:
- `http://localhost:8086`

---

## Example Test Request

Use this request to trigger the full PM2.5 flow:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5000/api/ReadingsMqtt/sensor-1" `
  -ContentType "application/json" `
  -Body '{
    "utc": 1713988800,
    "temperatureC": 27.5,
    "humidityPercent": 45.2,
    "eCo2Ppm": 600,
    "tVocPpb": 120,
    "pressureHpa": 1012.8,
    "pm25": 77.0,
    "fireAlarm": true
  }'
```

---

## How to Verify the End-to-End Flow

### 1. Subscribe to MQTT topics

Raw readings:

```powershell
docker exec -it mosquitto mosquitto_sub -v -t iot/pm25/readings
```

Filtered events:

```powershell
docker exec -it mosquitto mosquitto_sub -v -t iot/pm25/events
```

Notifications:

```powershell
docker exec -it mosquitto mosquitto_sub -v -t iot/pm25/notifications
```

### 2. Submit the test request
Use the example POST request shown above.

### 3. Check Analytics logs

```powershell
docker logs analytics-service --tail 50
```

Expected messages:
- connected to MQTT
- subscribed to `iot/pm25/events`
- event detected
- event written to InfluxDB
- notification response `OK`

### 4. Check Notification logs

```powershell
docker logs notification-service --tail 50
```

Expected messages:
- gRPC server running
- MQTT connection established
- PM2.5 alert received
- PM2.5 notification published

### 5. Check InfluxDB
Open `http://localhost:8086`, choose:
- bucket: `PM25-bucket`
- measurement: `pm25_events`

If no data appears immediately:
- refresh the browser
- widen the selected time range in Data Explorer

---

## Notes About Time

The MQTT event uses `timestampUtc` derived from the incoming `utc` field.

That means the event timestamp may reflect the source reading time rather than the current machine time.

If you do not see the record immediately in InfluxDB Data Explorer, it is often enough to:
- refresh the browser
- set a wider time range

---

## Why PM2.5 Instead of SmokeLevel?

The raw reading model inherited from Project 1 contains a real `pm25` field.  
To make the domain model consistent with the actual data, this project now uses **PM2.5** as the main event signal instead of the earlier placeholder name `smokeLevel`.

This makes the pipeline semantically cleaner:
- DataService stores raw `pm25`
- Gateway publishes `pm25`
- eKuiper filters by `pm25`
- Analytics stores `pm25_events`
- Notification emits `PM25_ALERT`

---

## Summary

This project demonstrates:
- event-driven IoT processing
- MQTT-based communication
- stream filtering with eKuiper
- time-series event storage with InfluxDB
- cross-service gRPC communication
- multi-technology microservices in Docker

The final system successfully performs an end-to-end PM2.5 event workflow:
1. raw reading received,
2. raw reading stored,
3. MQTT message published,
4. event filtered by eKuiper,
5. event stored in InfluxDB,
6. notification sent through gRPC,
7. notification published to MQTT.
