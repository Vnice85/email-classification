"use client";

import { useState } from "react";
import { X, Send, Save, Trash2, User } from "lucide-react";
import { sendEmail, createDraftEmail, createGuestEmail } from "@/services/api";
import { getCurrentUser, isGuestMode } from "@/utils/auth";
import Swal from "sweetalert2";

type Props = {
  onClose: () => void;
  onCompose?: (emailData?: any) => void;
  isGuest?: boolean;
};

export default function ComposeEmailModal({
  onClose,
  onCompose,
  isGuest,
}: Props) {
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [subject, setSubject] = useState("");
  const [content, setContent] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [notification, setNotification] = useState<{
    type: "success" | "error" | "info";
    message: string;
  } | null>(null);

  const currentUser = getCurrentUser();
  const guestMode = isGuest ?? isGuestMode();

  const handleSend = async () => {
    if (!to.trim() || !subject.trim()) {
      Swal.fire({
        icon: "error",
        title: "Lỗi",
        text: "Vui lòng điền đầy đủ người nhận và tiêu đề.",
      });
      return;
    }

    setIsLoading(true);
    try {
      if (guestMode) {
        // Guest mode: use guest API
        const emailData = {
          from: from.trim(),
          to: to.trim(),
          subject: subject.trim(),
          body: content.trim(),
        };
        await createGuestEmail(emailData);
        Swal.fire({
          position: "top-end",
          icon: "success",
          title: "Email đã được thêm thành công!",
          showConfirmButton: false,
          timer: 1500,
        });
        onCompose?.(emailData);
      } else {
        // Authenticated mode: use regular API
        const payload = {
          from: from.trim(),
          toAddress: to.trim(),
          subject: subject.trim(),
          body: content.trim(),
        };
        await sendEmail(payload);
        Swal.fire({
          position: "top-end",
          icon: "success",
          title: "Email đã được gửi thành công!",
          showConfirmButton: false,
          timer: 1500,
        });
        onCompose?.(payload);
      }

      // Reset form
      setTo("");
      setSubject("");
      setContent("");
      setTimeout(() => onClose(), 1500);
    } catch (error) {
      console.error("Failed to send email:", error);
      Swal.fire({
        position: "top-end",
        icon: "error",
        title: "Gửi email thất bại",
        showConfirmButton: false,
        timer: 1500,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleDraft = async () => {
    if (guestMode) {
      Swal.fire({
        icon: "info",
        title: "Thông báo",
        text: "Tính năng lưu bản nháp chỉ dành cho người dùng đã đăng nhập.",
      });
      return;
    }

    if (!to.trim() && !subject.trim() && !content.trim()) {
      Swal.fire({
        icon: "warning",
        title: "Không có nội dung",
        text: "Vui lòng nhập ít nhất một trường để lưu bản nháp.",
      });
      return;
    }

    setIsLoading(true);
    try {
      const payload = {
        toAddress: to.trim(),
        subject: subject.trim(),
        body: content.trim(),
      };
      await createDraftEmail(payload);
      Swal.fire({
        position: "top-end",
        icon: "success",
        title: "Bản nháp đã được lưu thành công!",
        showConfirmButton: false,
        timer: 1500,
      });
      onCompose?.(payload);
      setTimeout(() => onClose(), 1500);
    } catch (error) {
      console.error("Failed to save draft email:", error);
      Swal.fire({
        icon: "error",
        title: "Lỗi",
        text: "Không thể lưu bản nháp. Vui lòng thử lại sau.",
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleDiscard = () => {
    if (to.trim() || subject.trim() || content.trim()) {
      if (confirm("Bạn có chắc chắn muốn hủy email này? Nội dung sẽ bị mất.")) {
        onClose();
      }
    } else {
      onClose();
    }
  };

  return (
    <div className="fixed inset-0 backdrop-blur-sm bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="max-w-4xl w-full bg-white rounded-xl shadow-xl border border-gray-200 overflow-hidden">
        {/* Notification */}
        {notification && (
          <div
            className={`px-4 py-3 text-sm font-medium ${
              notification.type === "success"
                ? "bg-green-100 text-green-800 border-green-200"
                : notification.type === "error"
                ? "bg-red-100 text-red-800 border-red-200"
                : "bg-blue-100 text-blue-800 border-blue-200"
            } border-b`}
          >
            {notification.message}
          </div>
        )}

        {/* Header */}
        <div className="flex justify-between items-center p-6 border-b border-gray-200">
          <div>
            <h2 className="text-lg font-semibold text-gray-800">
              {guestMode ? "Thêm email mới" : "Soạn email"}
            </h2>
            {guestMode && (
              <p className="text-sm text-orange-600 mt-1">
                Chế độ khách - Tính năng hạn chế
              </p>
            )}
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            disabled={isLoading}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="p-6 space-y-6">
          {/* From */}
          <div className={guestMode ? "" : "hidden"}>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Gửi từ <span className="text-red-500">*</span>
            </label>
            <input
              type="email"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="recipient@example.com"
              disabled={isLoading}
              required
            />
          </div>

          {/* To */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Đến <span className="text-red-500">*</span>
            </label>
            <input
              type="email"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="recipient@example.com"
              disabled={isLoading}
              required
            />
          </div>

          {/* Subject */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Tiêu đề <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              placeholder="Nhập tiêu đề email..."
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              disabled={isLoading}
              required
            />
          </div>

          {/* Content */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Nội dung
            </label>
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              rows={10}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
              placeholder="Nhập nội dung email..."
              disabled={isLoading}
            />
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-between items-center p-6 border-t border-gray-200 bg-gray-50">
          <div className="text-sm text-gray-500">
            {guestMode ? (
              <span>💡 Đăng nhập để sử dụng đầy đủ tính năng</span>
            ) : (
              <span>Ctrl + Enter để gửi nhanh</span>
            )}
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={handleDiscard}
              disabled={isLoading}
              className="flex items-center gap-2 px-3 py-2 text-sm text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors disabled:opacity-50"
              title="Hủy"
            >
              <Trash2 className="w-4 h-4" />
              Hủy
            </button>

            {!guestMode && (
              <button
                onClick={handleDraft}
                disabled={isLoading}
                className="flex items-center gap-2 px-3 py-2 text-sm text-blue-600 hover:bg-blue-50 rounded-lg transition-colors disabled:opacity-50"
                title="Lưu bản nháp"
              >
                <Save className="w-4 h-4" />
                Lưu nháp
              </button>
            )}

            <button
              onClick={handleSend}
              disabled={isLoading || !to.trim() || !subject.trim()}
              className="flex items-center gap-2 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <Send className="w-4 h-4" />
              {isLoading ? "Đang xử lý..." : guestMode ? "Thêm email" : "Gửi"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
