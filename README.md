P2 – IoT Smoke Detection (SOA Project 2)

Author

Dušan Sotirov
Faculty of Electronic Engineering – Niš
Service-Oriented Architecture Project

Overview

This project represents the second phase of the IoT Smoke Detection system built using a Service-Oriented Architecture (SOA).

The system processes IoT sensor readings in real-time using MQTT and stream processing, detects smoke alert events, stores them in a time-series database, and triggers notifications via gRPC.

This project integrates and extends Project 1 API Gateway into a full event-driven microservices system.

Architecture
Client
   ↓
API Gateway (from P1)
   ↓ (MQTT publish)
Mosquitto Broker
   ↓
eKuiper (stream processing & filtering)
   ↓ (MQTT publish events)
Analytics Service (.NET 10)
   ↓
InfluxDB (event storage)
   ↓
Notification Service (Node.js gRPC)
Microservices
1️⃣ API Gateway (from P1)

Receives HTTP POST /api/readings

Publishes sensor data to MQTT topic:

iot/smoke/readings
2️⃣ Mosquitto (MQTT Broker)

Distributes messages between services

Topics used:

iot/smoke/readings

iot/smoke/events

3️⃣ eKuiper (Stream Processing Engine)
Stream:
readings
(deviceId string, temperature float, smokeLevel float, timestampUtc string)
Rule:
SELECT deviceId, temperature, smokeLevel, timestampUtc, "SMOKE_ALERT" as type
FROM readings
WHERE smokeLevel >= 70

Publishes filtered events to:

iot/smoke/events
4️⃣ Analytics Service (.NET 10)

Subscribes to:

iot/smoke/events

Deserializes payload (supports JSON object & JSON array)

Stores detected events in InfluxDB

Calls Notification Service via gRPC

5️⃣ InfluxDB

Stores detected smoke events

Measurement:

smoke_events

UI available at:

http://localhost:8086
6️⃣ Notification Service (Node.js + gRPC)

Receives smoke alerts

Logs confirmation response:

Notification response: OK
Docker Orchestration

All services run via:

docker compose up -d --build

Services:

mosquitto

ekuiper

influxdb

analytics-service

notification-service

api-gateway

How to Test
Send reading via API Gateway
POST /api/readings

Example JSON:

{
  "deviceId": "sensor-1",
  "temperature": 28.2,
  "smokeLevel": 80,
  "timestampUtc": "2026-02-19T21:00:00Z"
}
Expected Flow

Gateway publishes to iot/smoke/readings

eKuiper filters (smokeLevel >= 70)

Event published to iot/smoke/events

Analytics stores event in InfluxDB

gRPC notification triggered

Technologies Used

ASP.NET Core (.NET 10)

Node.js

MQTT (Mosquitto)

eKuiper (Stream Processing)

InfluxDB 2.7

gRPC

Docker & Docker Compose

Key Concepts Demonstrated

Event-driven architecture

MQTT publish/subscribe pattern

Stream processing with SQL-like rules

Microservice communication via gRPC

Time-series data storage

Containerized distributed system

Cross-language microservices (.NET + Node.js)

System Capabilities

Real-time smoke detection

Stream-based filtering

Event storage and querying

Notification triggering

Clean service separation

Extensible for production-scale IoT systems

