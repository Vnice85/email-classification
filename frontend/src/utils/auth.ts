"use client";

import { createGuestSession, clearGuestId } from "@/services/api";

// Guest session management
export function getGuestId(): string | null {
  return localStorage.getItem("guestId");
}

export function setGuestId(guestId: string): void {
  localStorage.setItem("guestId", guestId);
}

export function isGuestMode(): boolean {
  const token = localStorage.getItem("jwtToken");
  const guestId = localStorage.getItem("guestId");
  return !!guestId && !token;
}

export function isAuthenticatedUser(): boolean {
  const token = localStorage.getItem("jwtToken");
  const expiresAt = localStorage.getItem("expiresAt");

  if (!token || !expiresAt) return false;

  const expireTime = new Date(expiresAt).getTime();
  const now = new Date().getTime();
  return expireTime > now; // còn hạn thì hợp lệ
}

// Kiểm tra user có session hợp lệ không (guest hoặc authenticated)
// Hàm này dùng để kiểm tra có thể truy cập app không
export function isAuthenticated(): boolean {
  return isAuthenticatedUser() || isGuestMode();
}

// Hàm riêng để kiểm tra có cần hiển thị trang login không
export function needsLogin(): boolean {
  return !isAuthenticatedUser(); // Chỉ cần login khi chưa có JWT token
}

// Tạo guest session mới
export async function createGuestSessionIfNeeded(): Promise<string | null> {
  // Nếu đã có authenticated user, không cần guest session
  if (isAuthenticatedUser()) return null;

  // Nếu đã có guest session, trả về
  const existingGuestId = getGuestId();
  if (existingGuestId) return existingGuestId;

  try {
    const guestId = await createGuestSession();
    return guestId;
  } catch (error) {
    console.error("Failed to create guest session:", error);
    return null;
  }
}

// Mở popup đăng nhập với Google
export function loginWithGoogle(callback?: () => void) {
  const popup = window.open(
    "https://localhost:44366/auth/login",
    "googleLogin",
    "width=500,height=600"
  );

  const listener = (e: MessageEvent) => {
    const d = e.data;
    if (d.jwt) {
      // Clear guest session when user logs in
      clearGuestId();

      // Set authenticated user data
      localStorage.setItem("jwtToken", d.jwt);
      localStorage.setItem("userName", d.userName || "");
      localStorage.setItem("profileImage", d.profileImage || "");
      localStorage.setItem("expiresAt", d.expiresAt || "");
      localStorage.setItem("userId", d.userId || "");

      window.removeEventListener("message", listener); // cleanup
      popup?.close();
      callback?.();
    }
  };

  window.addEventListener("message", listener);

  // Handle popup closed manually
  const checkClosed = setInterval(() => {
    if (popup?.closed) {
      clearInterval(checkClosed);
      window.removeEventListener("message", listener);
    }
  }, 1000);
}

// Đăng xuất: xóa token và tạo guest session mới
export async function logout() {
  localStorage.clear();

  // Tạo guest session mới sau khi logout
  try {
    await createGuestSession();
  } catch (error) {
    console.error("Failed to create guest session after logout:", error);
  }

  window.location.reload();
}

// Chuyển từ guest sang authenticated mode
export async function upgradeFromGuest(callback?: () => void) {
  loginWithGoogle(() => {
    // Sau khi đăng nhập thành công, có thể sync dữ liệu guest nếu cần
    callback?.();
  });
}

// Lấy JWT hiện tại
export function getJwt(): string | null {
  return localStorage.getItem("jwtToken");
}

// Lấy thông tin user hiện tại
export function getCurrentUser() {
  if (isAuthenticatedUser()) {
    return {
      type: "authenticated" as const,
      userName: localStorage.getItem("userName") || "",
      profileImage: localStorage.getItem("profileImage") || "",
      email: "", // Có thể thêm email nếu API trả về
    };
  } else if (isGuestMode()) {
    return {
      type: "guest" as const,
      guestId: getGuestId(),
      userName: "Khách",
      profileImage: "",
    };
  }
  return null;
}

// Làm mới token nếu gần hết hạn
export async function autoRefreshToken() {
  if (!isAuthenticatedUser()) return;

  const expiresAt = localStorage.getItem("expiresAt");
  if (!expiresAt) return;

  const expireTime = new Date(expiresAt).getTime();
  const now = new Date().getTime();

  if (expireTime - now < 5 * 60 * 1000) {
    // Nếu còn dưới 5 phút
    try {
      const res = await fetch("https://localhost:44366/auth/refreshtoken", {
        headers: {
          Authorization: `Bearer ${getJwt()}`,
        },
      });
      const data = await res.json();
      if (data.jwtAccessToken) {
        localStorage.setItem("jwtToken", data.jwtAccessToken);
        localStorage.setItem("expiresAt", data.expiresAt);
      }
    } catch (error) {
      console.error("Failed to refresh token:", error);
      // Nếu refresh token thất bại, logout và tạo guest session
      await logout();
    }
  }
}

// Kiểm tra và tự động refresh token
export function startTokenRefreshTimer() {
  // Chỉ chạy cho authenticated users
  if (!isAuthenticatedUser()) return;

  const interval = setInterval(async () => {
    if (isAuthenticatedUser()) {
      await autoRefreshToken();
    } else {
      clearInterval(interval);
    }
  }, 60000); // Kiểm tra mỗi phút

  return interval;
}

// Utility function để redirect dựa trên auth state
export function redirectBasedOnAuth() {
  if (isAuthenticatedUser()) {
    // Đã login → về inbox chính
    window.location.href = "/inbox";
  } else if (isGuestMode()) {
    // Chế độ khách → về inbox dành cho khách
    window.location.href = "/guest/inbox";
  } else {
    // Chưa có gì → tạo guest session rồi về trang chủ
    createGuestSessionIfNeeded().then(() => {
      window.location.href = "/";
    });
  }
}

// Hook để sử dụng trong React components
export function useAuth() {
  const user = getCurrentUser();
  const isGuest = isGuestMode();
  const isAuth = isAuthenticatedUser();

  return {
    user,
    isGuest,
    isAuthenticated: isAuth,
    hasSession: isAuthenticated(),
    login: loginWithGoogle,
    logout,
    upgradeFromGuest,
  };
}
