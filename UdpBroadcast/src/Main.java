import java.io.IOException;
import java.net.NetworkInterface;
import java.net.SocketException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class Main {
    public static final int PORT = 7777;

    private static final String NAME = "Bobrov";

    public static final Object sync = new Object();

    public static void main(String[] args) {
        String name = NAME;
        if (args.length > 0) {
            name = args[0];
        }
        List<byte[]> myMacs = new ArrayList<>();
        try {
            for (NetworkInterface networkInterface : Collections.list(NetworkInterface.getNetworkInterfaces())) {
                myMacs.add(networkInterface.getHardwareAddress());
            }
        } catch (SocketException e) {
            e.printStackTrace();
            System.exit(1);
        }

        Peers peers = new Peers(myMacs);

        final String os = System.getProperty("os.name");
        boolean isWin = os.contains("Windows");

        try (BroadcastSender sender = new BroadcastSender(name);
            BroadcastReceiver receiver = new BroadcastReceiver(peers)) {
            sender.start();
            receiver.start();
            while (true) {
                if (!isWin) {
                    System.out.print("\u001b[2J");
                    System.out.flush();
                } else {
                    for (int i = 0; i < 50; i++) {
                        System.out.println(); // I love Windows!
                    }
                }
                List<String> peerStrings;
                synchronized (sync) {
                    peerStrings = peers.getPeers();
                }
                peerStrings.stream().forEachOrdered(System.out::println);
                String separator = "_________________________";
                System.out.println(separator);
                try {
                    Thread.sleep(2000);
                } catch (InterruptedException ignored) {
                }
            }
        }
    }
}
