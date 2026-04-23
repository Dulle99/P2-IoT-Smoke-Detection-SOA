const path = require("path");
const grpc = require("@grpc/grpc-js");
const protoLoader = require("@grpc/proto-loader");
const mqtt = require("mqtt");

// Load the gRPC proto file
const PROTO_PATH = path.join(__dirname, "notification.proto");

// MQTT configuration
const MQQT_BROKER_URL = process.env.MQTT_BROKER_URL || "mqtt://localhost:1883";
const MQTT_TOPIC_NOTIFICATIONS = process.env.MQTT_TOPIC_NOTIFICATIONS || "iot/smoke/notifications";

const mqttClient = mqtt.connect(MQQT_BROKER_URL);

mqttClient.on("connect", () => {
    console.log(`Connected to MQTT broker at ${MQQT_BROKER_URL}`);
});
mqttClient.on("error", (err) => {
    console.error("Failed to connect to MQTT broker:", err);
});


const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
  keepCase: true,
  longs: String,
  enums: String,
  defaults: true,
    oneofs: true,
});

const notificationProto = grpc.loadPackageDefinition(packageDefinition).notification;

function sendSmokeAlert(call, callback) {
    const req = call.request;
    console.log("Received smoke alert via gRPC:", req);

    const notification = {
        type: "SMOKE_ALERT",
        deviceId: req.deviceId,
        smokeLevel: req.smokeLevel,
        temperature: req.temperature,
        timestampUtc: req.timestampUtc,
        message: `Smoke alert from device ${req.deviceId} detected smoke level ${req.smokeLevel} at ${new Date(req.timestampUtc).toISOString()}`,
    };

    mqttClient.publish(MQTT_TOPIC_NOTIFICATIONS, JSON.stringify(notification), { qos: 1 }, (err) => {
        if(err) {
            console.error("Failed to publish MQTT message:", err);
            callback(null, {status: "ERROR", message: "Failed to publish MQTT message"});
            return;
        }

        console.log("Published smoke alert to MQTT topic:", MQTT_TOPIC_NOTIFICATIONS);
        callback(null, { success: true, message: "Smoke alert sent successfully" });
    });
}

function main() {
    const server = new grpc.Server();

    server.addService(notificationProto.NotificationService.service, {
        sendSmokeAlert: sendSmokeAlert,
    });

    const addr = "0.0.0.0:3000";
    server.bindAsync(addr, grpc.ServerCredentials.createInsecure(), (err, port) => {
        if(err) {
            console.error("Failed to bind gRPC server:", err);
            return;
        }
        console.log(`gRPC server is running on ${addr}`);
        server.start();
    });
}

main();