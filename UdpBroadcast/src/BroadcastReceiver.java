import java.io.Closeable;
import java.io.IOException;
import java.net.*;
import java.util.Arrays;

public class BroadcastReceiver implements Closeable {
    private final Thread worker;
    private volatile boolean isRunning;
    private boolean started;

    public BroadcastReceiver(Peers peers) {
        worker = new Thread(() -> {
            try (DatagramSocket socket = new DatagramSocket(Main.PORT)) {
                socket.setSoTimeout(5000);
                byte[] packet = new byte[512];
                DatagramPacket dp = new DatagramPacket(packet, packet.length);

                while (isRunning) {
                    try {
                        socket.receive(dp);
                        if (!(dp.getAddress() instanceof Inet4Address) ||
                                !Arrays.equals(dp.getAddress().getAddress(), Arrays.copyOfRange(packet, 0, 4))) {
                            continue;
                        }
                        peers.registerPacket(packet);
                    } catch (SocketTimeoutException ignored) { }
                }
            } catch (IOException e) {
                e.printStackTrace();
                System.exit(1);
            }
        });
    }

    public void start() {
        if (started) {
            throw new IllegalStateException("Already started");
        }
        started = true;
        isRunning = true;
        worker.start();
    }

    @Override
    public void close() {
        isRunning = false;
        worker.interrupt();
    }
}
