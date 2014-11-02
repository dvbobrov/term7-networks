import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.nio.charset.Charset;
import java.util.Arrays;

public class PeerData implements Cloneable {
    private final InetAddress ip;
    private final byte[] mac;
    private final String name;

    private volatile long lastPacket;

    private volatile int history;

    public PeerData(InetAddress ip, byte[] mac, String name) {
        this.ip = ip;
        this.mac = mac;
        this.name = name;
        history = 0b11111_11111;
        lastPacket = System.currentTimeMillis();
    }

    public byte[] getMac() {
        return mac;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        PeerData peerData = (PeerData) o;

        if (!Arrays.equals(mac, peerData.mac)) return false;

        return true;
    }

    @Override
    public int hashCode() {
        return Arrays.hashCode(mac);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append(ip.toString()).append("\t");

        appendByte(sb, mac[0]);
        for (int i = 1; i < mac.length; i++) {
            sb.append(':');
            appendByte(sb, mac[i]);
        }
        sb.append("\t");
        sb.append(name);
        sb.append("\t");
        sb.append(System.currentTimeMillis() - lastPacket);
        sb.append("\t");
        sb.append(getSkippedPackets());

        return sb.toString();
    }

    public void registerPacket() {
        synchronized (Main.sync) {
            lastPacket = System.currentTimeMillis();
        }
    }

    public boolean isAlive() {
        synchronized (Main.sync) {
            boolean gotPacket = System.currentTimeMillis() - lastPacket <= 2000L;
            history = ((history << 1) | (gotPacket ? 1 : 0)) & 0b11111_11111;
            return history != 0;
        }
    }

    public int getSkippedPackets() {
        return 10 - Integer.bitCount(history);
    }

    public static PeerData parse(byte[] bytes) throws UnknownHostException {
        InetAddress ip = Inet4Address.getByAddress(Arrays.copyOfRange(bytes, 0, 4));
        byte[] mac = Arrays.copyOfRange(bytes, 4, 10);

        Charset charset = Charset.forName("UTF-8");
        int i;
        for (i = 10; i < bytes.length && bytes[i] != 0; i++);
        if (i == 10 || (i == bytes.length && bytes[i - 1] != 0)) { // not null-terminated string or empty string
            return null;
        }
        String name = new String(bytes, 10, i - 10, charset);
        if (name.length() > 100 || !name.matches("[\\w\\s\\d_-]+")) {
            return null;
        }

        return new PeerData(ip, mac, name);
    }

    private final static char[] hexArray = "0123456789ABCDEF".toCharArray();
    private static void appendByte(StringBuilder sb, byte b) {
        int v = b & 0xFF;
        sb.append(hexArray[v >>> 4]);
        sb.append(hexArray[v & 0x0F]);
    }

    public InetAddress getIp() {
        return ip;
    }
}
