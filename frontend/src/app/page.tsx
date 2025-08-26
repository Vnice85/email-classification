"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import {
  isAuthenticatedUser,
  isGuestMode,
  createGuestSessionIfNeeded,
} from "@/utils/auth";
import Image from "next/image";

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    if (isAuthenticatedUser()) {
      router.push("/inbox");
    } else if (isGuestMode()) {
      router.push("/");
    } else {
      createGuestSessionIfNeeded(); // tạo guest nhưng không redirect
    }
  }, [router]);

  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-400 to-pink-300 px-4 sm:px-6 lg:px-12 py-6 flex flex-col font-inter">
      <div className="flex-1 flex flex-col justify-between gap-6 bg-transparent rounded-2xl shadow-lg px-4 sm:px-6 lg:px-12 py-6 my-4 w-full">
        {/* Header Section */}
        <header className="w-full max-w-6xl mx-auto text-center py-8 px-6 md:px-12">
          <h1 className="text-4xl md:text-5xl font-bold leading-tight mb-4 text-transparent bg-clip-text bg-gradient-to-br from-blue-600 via-purple-500 to-pink-500 drop-shadow-md">
            Nền tảng toàn diện <br />
            cho phân loại Email thông minh
          </h1>
          <p className="text-base md:text-lg text-gray-700 max-w-2xl mx-auto font-medium leading-relaxed">
            Giúp bạn quản lý hộp thư hiệu quả hơn bằng Machine Learning: <br />
            chống spam, lọc phishing, phân loại nội dung.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row justify-center gap-4">
            <button
              onClick={() => router.push("/login")}
              className="w-40 py-3 rounded-full text-base font-semibold text-white bg-gradient-to-br from-blue-500 to-purple-500 hover:brightness-110 hover:shadow-lg hover:shadow-purple-400/50 transition-all duration-300 text-center"
            >
              Đăng nhập
            </button>
            <button
              onClick={() => router.push("/guest/inbox")}
              className="w-40 py-3 rounded-full text-base font-semibold text-blue-900 bg-gradient-to-br from-white/40 to-white/10 border border-blue-600 hover:shadow-lg hover:shadow-blue-300/50 transition-all duration-300 text-center"
            >
              Chế độ khách
            </button>
          </div>
        </header>

        {/* Feature Section */}
        <section className="py-4 px-4 flex-1">
          <div className="max-w-6xl mx-auto grid md:grid-cols-3 gap-6 text-center items-stretch h-full">
            <div className="flex flex-col justify-between bg-gradient-to-br from-white/20 to-white/80 backdrop-blur-xl border border-white/40 rounded-xl shadow-2xl hover:shadow-blue-200/80 transition-all duration-300 hover:scale-[1.02] px-6 py-6 text-gray-800 h-full min-h-[320px]">
              <Image
                src="/icons/smart-label.png"
                alt="Auto Labeling"
                width={128}
                height={128}
                className="mx-auto mb-4 drop-shadow-md"
              />
              <h3 className="text-2xl font-bold mb-3 text-transparent bg-clip-text bg-gradient-to-br from-blue-600 via-purple-500 to-pink-500 drop-shadow-md">
                Tự động phân loại
              </h3>
              <p className="text-gray-700 font-medium leading-relaxed">
                Gắn nhãn thông minh theo nội dung: spam, công việc, cá nhân,
                khẩn cấp...
              </p>
            </div>
            <div className="flex flex-col justify-between bg-gradient-to-br from-white/20 to-white/80 backdrop-blur-xl border border-white/40 rounded-xl shadow-2xl hover:shadow-blue-200/80 transition-all duration-300 hover:scale-[1.02] px-6 py-6 text-gray-800 h-full min-h-[320px]">
              <Image
                src="/icons/phishing-alert.png"
                alt="Phishing Protection"
                width={128}
                height={128}
                className="mx-auto mb-4 drop-shadow-md"
              />
              <h3 className="text-2xl font-bold mb-3 text-transparent bg-clip-text bg-gradient-to-br from-blue-600 via-purple-500 to-pink-500 drop-shadow-md">
                Chống lừa đảo
              </h3>
              <p className="text-gray-700 font-medium leading-relaxed">
                Cảnh báo email nghi ngờ giả mạo hoặc lừa đảo.
              </p>
            </div>
            <div className="flex flex-col justify-between bg-gradient-to-br from-white/20 to-white/80 backdrop-blur-xl border border-white/40 rounded-xl shadow-2xl hover:shadow-blue-200/80 transition-all duration-300 hover:scale-[1.02] px-6 py-6 text-gray-800 h-full min-h-[320px]">
              <Image
                src="/icons/search-mail.png"
                alt="Search & Suggestion"
                width={128}
                height={128}
                className="mx-auto mb-4 drop-shadow-md"
              />
              <h3 className="text-2xl font-bold mb-3 text-transparent bg-clip-text bg-gradient-to-br from-blue-600 via-purple-500 to-pink-500 drop-shadow-md">
                Tìm kiếm thông minh
              </h3>
              <p className="text-gray-700 font-medium leading-relaxed">
                Tìm theo người gửi, từ khóa hoặc nhãn với tốc độ cao.
              </p>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
