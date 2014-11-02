import java.io.Closeable;
import java.io.IOException;
import java.net.*;
import java.nio.charset.Charset;
import java.util.Collections;
import java.util.Enumeration;
import java.util.List;

public class BroadcastSender implements Closeable {
    private final Thread worker;
    private volatile boolean isRunning;
    private boolean started;

    public BroadcastSender(String name) {
        name = name + '\0';
        final byte[] bytes = name.getBytes(Charset.forName("UTF-8"));
        final int nameLen = bytes.length;
        worker = new Thread(() -> {
            try (DatagramSocket socket = new DatagramSocket()) {
                while (isRunning) {
                    try {
                        Enumeration<NetworkInterface> interfaces = NetworkInterface.getNetworkInterfaces();

                        for (NetworkInterface networkInterface : Collections.list(interfaces)) {
                            if (networkInterface == null ||
                                    networkInterface.isLoopback() ||
                                    !networkInterface.isUp()) {
                                continue;
                            }
                            List<InterfaceAddress> ips = networkInterface.getInterfaceAddresses();
                            byte[] mac = networkInterface.getHardwareAddress();
                            for (InterfaceAddress ip : ips) {
                                InetAddress address = ip.getAddress();
                                if (!(address instanceof Inet4Address)) {
                                    continue;
                                }
                                byte[] data = new byte[4 + 6 + nameLen];
                                System.arraycopy(address.getAddress(), 0, data, 0, 4);
                                System.arraycopy(mac, 0, data, 4, 6);
                                System.arraycopy(bytes, 0, data, 4 + 6, bytes.length);
                                DatagramPacket packet = new DatagramPacket(data, data.length, ip.getBroadcast(), Main.PORT);
                                socket.send(packet);
                            }
                        }
                    } catch (IOException e) {
                        System.err.println(e);
                    }

                    try {
                        Thread.sleep(1900L);
                    } catch (InterruptedException e) {
                        // reset interrupted state
                        Thread.currentThread().interrupt();
                        return;
                    }
                }
            } catch (SocketException e) {
                System.err.println(e);
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
        try {
            worker.join();
        } catch (InterruptedException ingored) {
        }
    }
}
