import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection | null = null;

export const startConnection = (
  onEmailLabelChanged: (emailId: string, newLabel: string) => void
) => {
  // Lấy userId từ localStorage
  const userId = localStorage.getItem("userId");
  if (!userId) {
    console.error("No userId found in localStorage");
    return null;
  }

  // Đảm bảo chỉ có 1 kết nối
  if (connection) return connection;

  connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:44366/emailhub", {
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets,
      accessTokenFactory: () => localStorage.getItem("token") || "",
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        if (retryContext.elapsedMilliseconds < 60000) {
          return 2000;
        }
        return null;
      },
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Xử lý nhận message
  connection.on(
    "ReceiveNewLabels",
    (data: { emailId: string; newLabel: string }) => {
      console.log("Received update for email:", data.emailId);
      onEmailLabelChanged(data.emailId, data.newLabel);
    }
  );

  // Xử lý sự kiện kết nối
  connection.onclose((error) => {
    console.log("Connection closed:", error);
    connection = null;
  });

  // Bắt đầu kết nối
  const start = async () => {
    try {
      await connection!.start();
      console.log("SignalR connected. User ID:", userId);

      // Gửi userId lên server để mapping
      await connection!.invoke("RegisterUser", userId);
    } catch (err) {
      console.error("Connection error:", err);
      setTimeout(start, 5000);
    }
  };

  start();

  return connection;
};

export const stopConnection = async () => {
  if (connection) {
    await connection.stop();
    connection = null;
  }
};
