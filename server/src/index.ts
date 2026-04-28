import { Server } from "colyseus";
import { monitor } from "@colyseus/monitor";
import { createServer } from "http";
import express from "express";
import { FallGuysRoom } from "./rooms/FallGuysRoom";

const PORT = Number(process.env.PORT ?? 2567);

const app        = express();
const httpServer = createServer(app);

app.use(express.json());

// Colyseus dashboard at /colyseus (dev only)
app.use("/colyseus", monitor());

const gameServer = new Server({ server: httpServer });

gameServer
    .define("fall_guys_room", FallGuysRoom)
    .enableRealtimeListing();

httpServer.listen(PORT, () => {
    console.log(`[GameServer] ws://localhost:${PORT}`);
    console.log(`[Monitor]   http://localhost:${PORT}/colyseus`);
});
