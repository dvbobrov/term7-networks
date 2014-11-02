import java.net.NetworkInterface;
import java.net.SocketException;
import java.net.UnknownHostException;
import java.util.*;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.stream.Collectors;

public class Peers {
    private final Map<PeerData, PeerData> data = new HashMap<>();
    ScheduledThreadPoolExecutor pool = (ScheduledThreadPoolExecutor) Executors.newScheduledThreadPool(3);
    private final List<byte[]> myMacs;
    private static int ifCount;

    static  {
        try {
            ifCount = Collections.list(NetworkInterface.getNetworkInterfaces()).size();
        } catch (SocketException e) {
            System.exit(1);
        }
    }

    public Peers(List<byte[]> myMacs) {
        this.myMacs = myMacs;
    }

    public synchronized void registerPacket(byte[] bytes) {
        try {
            PeerData peer = PeerData.parse(bytes);
            if (peer == null) return;
            if (myMacs.stream().anyMatch(addr -> Arrays.equals(addr, peer.getMac()))) {
                return;
            }
            long count = data.keySet().stream()
                    .filter(p -> p.getIp().equals(peer.getIp()))
                    .count();
            if (count > ifCount) return;
            if (data.containsKey(peer)) {
                data.get(peer).registerPacket();
            } else {
                data.put(peer, peer);
                pool.schedule(new CheckHistoryRunnable(peer), 2, TimeUnit.SECONDS);
            }

        } catch (UnknownHostException ignored) {
        }
    }

    public synchronized List<String> getPeers() {
        synchronized (Main.sync) {
            return data.keySet().stream()
                    .sorted((a, b) -> Integer.compare(a.getSkippedPackets(), b.getSkippedPackets()))
                    .map(PeerData::toString)
                    .collect(Collectors.toList());
        }
    }

    private final class CheckHistoryRunnable implements Runnable {
        private final PeerData peer;

        private CheckHistoryRunnable(PeerData peer) {
            this.peer = peer;
        }

        @Override
        public void run() {
            if (peer.isAlive()) {
                pool.schedule(this, 2, TimeUnit.SECONDS);
            } else {
                data.remove(peer);
            }
        }
    }
}
