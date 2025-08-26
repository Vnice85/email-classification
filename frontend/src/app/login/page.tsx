"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { isAuthenticatedUser, loginWithGoogle } from "@/utils/auth";

export default function LoginPage() {
  const router = useRouter();

  useEffect(() => {
    // Chỉ redirect khi user đã authenticated thật sự (có JWT token)
    // Không redirect khi chỉ có guest session
    if (isAuthenticatedUser()) {
      router.replace("/inbox"); // Đã login thì về inbox
    }
  }, [router]);

  const handleLogin = () => {
    loginWithGoogle(() => {
      router.push("/inbox"); // Sau khi login thành công, đi đến inbox
    });
  };

  const handleBackToHome = () => {
    router.push("/"); // Quay về trang chủ (có thể dùng guest mode)
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gradient-to-br from-blue-200 to-pink-300 p-4 font-inter">
      <div className="w-full max-w-sm bg-white/30 backdrop-blur-xl border border-white/40 rounded-xl shadow-2xl p-8 text-center space-y-6 text-gray-800">
        <h1 className="text-2xl font-bold tracking-tight">
          Đăng nhập với Google
        </h1>
        <p className="text-gray-600 text-sm leading-relaxed">
          Đăng nhập để sử dụng đầy đủ tính năng <br />
          phân loại email thông minh.
        </p>

        <div className="space-y-3">
          <button
            onClick={handleLogin}
            className="w-full py-3 rounded-full text-base font-semibold text-white bg-gradient-to-br from-blue-500 to-purple-500 hover:brightness-110 hover:shadow-lg hover:shadow-purple-400/50 transition-all duration-300"
          >
            Đăng nhập với Google
          </button>

          <button
            onClick={handleBackToHome}
            className="w-full py-2 rounded-full text-base font-medium text-blue-700 bg-white/50 border border-blue-300 hover:bg-white/70 hover:shadow-md transition-all duration-300"
          >
            Quay về trang chủ
          </button>
        </div>

        <div className="text-xs text-gray-500 mt-4">
          <p>Hoặc bạn có thể sử dụng</p>
          <button
            onClick={handleBackToHome}
            className="text-blue-600 hover:underline font-medium"
          >
            chế độ khách
          </button>
          <p>với tính năng hạn chế</p>
        </div>
      </div>
    </div>
  );
}
