const {MongoClient} = require('mongodb');

const MONGO_URL = process.env.MONGO_URL || 'mongodb://localhost:27017';
const DB_NAME = process.env.DB_NAME || 'smoke';

let client;
let db;

async function connectDb(){
    if(db) return db;

    client = new MongoClient(MONGO_URL);
    await client.connect();
    db=client.db(DB_NAME);

    console.log(`Connected to MongoDB: ${MONGO_URL}, db=${DB_NAME}`);
    return db;
}

function getDb(){
    if(!db) throw new Error("Database not connected. Call connectDb() first.");

    return db;
}

module.exports = {connectDb, getDb};
