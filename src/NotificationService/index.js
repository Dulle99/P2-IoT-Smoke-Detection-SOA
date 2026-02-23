const path = require("path");
const grpc = require("@grpc/grpc-js");
const protoLoader = require("@grpc/proto-loader");

const PROTO_PATH = path.join(__dirname, "notification.proto");

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

    callback(null, { status: "OK" });
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