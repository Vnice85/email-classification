"use client";
import { useState, useEffect } from "react";
import { Sidebar } from "@/components/Sidebar";
import EmailDetailModal from "@/components/EmailDetailModal";
import { Star } from "lucide-react";
import ComposeEmailModal from "@/components/ComposeEmailModal";
import React from "react";
import * as signalR from "@microsoft/signalr";
import { startConnection, stopConnection } from "@/services/signalr";
import {
  getEmails as getEmailsOriginal,
  getEmailDetail,
  syncEmails,
  searchEmails,
  classifyEmails,
} from "@/services/api";

type Email = {
  id: string;
  subject: string;
  sender: string;
  recipient: string;
  snippet: string;
  content?: string;
  date: string;
  isRead: boolean;
  labels: string[];
  direction: "SENT" | "RECEIVED" | "DRAFT";
  prediction?: {
    label: string;
    probability: number;
    prediction: number;
    confidenceLevel: string;
    flags: {
      urgentKeywords?: string[];
      suspiciousUrls?: string[];
    };
  };
};

const Page = () => {
  const [emails, setEmails] = useState<Email[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [selectedEmail, setSelectedEmail] = useState<Email | null>(null);
  const [searchContent, setSearchContent] = useState("");
  const [isEmailDetailOpen, setIsEmailDetailOpen] = useState(false);
  const [isClassify, setIsClassify] = useState(false);

  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;
  const [hasMore, setHasMore] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  const fetchEmails = async (page = 1) => {
    try {
      const response = await getEmailsOriginal({
        pageindex: page,
        pagesize: pageSize,
        labelname: "",
        directionname: "SENT",
      });

      const mappedEmails = response.map((item: any) => ({
        id: item.emailId,
        subject: item.subject || "(Không có tiêu đề)",
        sender: item.fromAddress || "Không rõ người gửi",
        recipient: item.toAddress || "Không rõ người gửi",
        snippet: item.snippet || "(Không có nội dung)",
        content:
          item.body ||
          item.details?.body ||
          item.snippet ||
          "(No content available)",
        date: item.sentDate || item.receivedDate || new Date().toISOString(),
        isRead: true,
        labels: item.labelName ? [item.labelName] : ["UNDEFINE"],
      }));

      if (page === 1) {
        setEmails(mappedEmails);
      } else {
        setEmails((prev) => [...prev, ...mappedEmails]);
      }

      if (mappedEmails.length < pageSize) setHasMore(false);
    } catch (error) {
      console.error("Failed to fetch emails:", error);
    }
  };

  const loadMoreEmails = () => {
    const nextPage = currentPage + 1;
    setCurrentPage(nextPage);
    setIsLoadingMore(true);
    fetchEmails(nextPage).finally(() => setIsLoadingMore(false));
  };

  useEffect(() => {
    let isMounted = true;

    const init = async () => {
      try {
        await syncEmails();
        await fetchEmails(1);

        const conn = startConnection((emailId, newLabel) => {
          if (!isMounted) return;
          setEmails((prev) =>
            prev.map((e) =>
              e.id === emailId ? { ...e, labels: [newLabel] } : e
            )
          );
        });

        // Kiểm tra kết nối sau 3s
        const checkConnection = setTimeout(() => {
          if (conn?.state !== signalR.HubConnectionState.Connected) {
            console.warn("Reconnecting SignalR...");
            conn?.start().catch(console.error);
          }
        }, 3000);

        return () => clearTimeout(checkConnection);
      } catch (error) {
        console.error("Initialization error:", error);
      }
    };

    init();

    return () => {
      isMounted = false;
      stopConnection().catch(console.error);
    };
  }, []);
  useEffect(() => {
    const fetchSearchResults = async () => {
      if (searchContent.trim() === "") return;
      try {
        const response = await searchEmails(1, pageSize, searchContent);
        const mappedEmails = response.map((item: any) => ({
          id: item.emailId,
          subject: item.subject || "(Không có tiêu đề)",
          sender: item.fromAddress || "Không rõ người gửi",
          recipient: item.toAddress || "Không rõ người nhận",
          snippet: item.snippet || "(Không có nội dung)",
          content:
            item.body ||
            item.details?.body ||
            item.snippet ||
            "(No content available)",
          date: item.sentDate || item.receivedDate || new Date().toISOString(),
          isRead: true,
          labels: item.labelName ? [item.labelName] : ["Chưa gắn nhãn"],
        }));
        setEmails(mappedEmails);
        setHasMore(false);
      } catch (error) {
        console.error("Lỗi tìm kiếm email:", error);
      }
    };

    fetchSearchResults();
  }, [searchContent]);

  useEffect(() => {
    if (isClassify) {
      const classify = async () => {
        try {
          const result = await classifyEmails();
          console.log("Kết quả phân loại:", result);
          fetchEmails(1);
        } catch (error) {
          console.error("Lỗi phân loại email:", error);
          alert("Không thể phân loại email. Vui lòng thử lại sau.");
        }
      };
      classify();
      setIsClassify(false);
    }
  }, [isClassify]);

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    );
  };

  const handleOpenEmailModal = () => {
    setIsEmailDetailOpen(true);
  };

  const handleEmailClick = async (emailId: string) => {
    try {
      const detail = await getEmailDetail(emailId.trim());

      const email: Email = {
        id: detail.emailId,
        subject: detail.subject || "(Không có tiêu đề)",
        sender: detail.fromAddress || "Không rõ người gửi",
        recipient: detail.toAddress || "Không rõ người nhận",
        snippet: detail.snippet || "",
        content: detail.body || "(Không có nội dung)",
        date:
          detail.sentDate || detail.receivedDate || new Date().toISOString(),
        isRead: true,
        labels: [detail.labelName || "Chưa gắn nhãn"],
        direction: detail.directionName || "RECEIVED",
        prediction: detail.details
          ? {
              label: detail.details.label,
              probability: detail.details.probability,
              prediction: detail.details.prediction,
              confidenceLevel: detail.details.confidenceLevel,
              flags: detail.details.details?.flags || {},
            }
          : undefined,
      };

      setSelectedEmail((prev) => ({
        ...prev,
        ...email,
      }));
    } catch (error) {
      console.error("Lỗi lấy nội dung email:", error);
    }
  };

  const [isLoadingClassify, setIsLoadingClassify] = useState(false);

  const handleClassifyClick = async () => {
    setIsLoadingClassify(true);
    const classifyPromise = classifyEmails().catch((error) => {
      console.error("Phân loại lỗi", error);
    });
    await Promise.all([
      classifyPromise,
      new Promise((resolve) => setTimeout(resolve, 3000)),
    ]);
    setIsLoadingClassify(false);
  };

  const [isLoadingSync, setIsLoadingSync] = useState(false);

  const handleSyncClick = async () => {
    setIsLoadingSync(true);
    const syncPromise = syncEmails().catch((error) => {
      console.error("Đồng bộ lỗi", error);
    });
    await Promise.all([
      syncPromise,
      new Promise((resolve) => setTimeout(resolve, 3000)),
    ]);
    setIsLoadingSync(false);
  };

  return (
    <div className="flex min-h-screen bg-gradient-to-br from-blue-100 to-purple-200 text-gray-800">
      <div className="sticky top-0 h-screen">
        <Sidebar
          onCompose={handleOpenEmailModal}
          setSearchContent={setSearchContent}
          setIsClassify={setIsClassify}
        />
      </div>
      <div className="flex-1 p-6 space-y-4 overflow-y-auto">
        <div className="flex">
          <button
            type="button"
            onClick={handleClassifyClick}
            className="relative flex items-center gap-2 text-gray-900 bg-white border border-gray-300 focus:outline-none hover:bg-gray-100 focus:ring-4 focus:ring-gray-100 font-medium rounded-lg text-sm px-5 py-2.5 me-2 mb-2 dark:bg-gray-800 dark:text-white dark:border-gray-600 dark:hover:bg-gray-700 dark:hover:border-gray-600 dark:focus:ring-gray-700"
          >
            {isLoadingClassify && (
              <svg
                className="animate-spin h-4 w-4 text-gray-500"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                ></circle>
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                ></path>
              </svg>
            )}
            {isLoadingClassify ? "Đang phân loại..." : "Phân loại toàn bộ"}
          </button>

          <button
            type="button"
            onClick={handleSyncClick}
            className="relative flex items-center gap-2 text-gray-900 bg-white border border-gray-300 focus:outline-none hover:bg-gray-100 focus:ring-4 focus:ring-gray-100 font-medium rounded-lg text-sm px-5 py-2.5 me-2 mb-2 dark:bg-gray-800 dark:text-white dark:border-gray-600 dark:hover:bg-gray-700 dark:hover:border-gray-600 dark:focus:ring-gray-700"
          >
            {isLoadingSync && (
              <svg
                className="animate-spin h-4 w-4 text-gray-500"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                ></circle>
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                ></path>
              </svg>
            )}
            {isLoadingSync ? "Đang đồng bộ..." : "Đồng bộ email"}
          </button>
        </div>

        {emails.map((email) => (
          <div
            key={email.id}
            className="border border-gray-300 rounded-md p-4 shadow-sm bg-white flex gap-4 items-start"
          >
            <div className="flex-1">
              <button onClick={() => handleEmailClick(email.id)}>
                <h3 className="text-lg font-semibold text-blue-700 hover:underline">
                  {email.subject}
                </h3>
              </button>
              <p className="text-sm text-gray-500 mb-1">
                Tới: {email.recipient}
              </p>
              <div
                className="text-sm text-gray-700"
                dangerouslySetInnerHTML={{
                  __html: email.content || email.snippet,
                }}
              />
              <div className="flex justify-between items-center text-xs text-gray-400 mt-2">
                <span>{new Date(email.date).toLocaleString()}</span>
                {email.labels.map((label, index) => {
                  const map = {
                    NORMAL: ["bg-green-100", "text-green-700"],
                    UNDEFINE: ["bg-gray-100", "text-gray-700"],
                    // NORMAL: ["hidden"],
                    // UNDEFINE: ["hidden"],
                    "SPAM/PHISHING": ["bg-red-100", "text-red-700"],
                  };
                  const upperLabel = label.toUpperCase() as keyof typeof map;
                  const [bgColor, textColor] = map[upperLabel] || [
                    "bg-blue-100",
                    "text-blue-700",
                  ];

                  return (
                    <span
                      key={index}
                      className={`${bgColor} ${textColor} px-2 py-0.5 rounded mr-1 text-sm pt-1`}
                    >
                      {label}
                    </span>
                  );
                })}
              </div>
            </div>
            <button onClick={() => console.log("Starred")}>...</button>
          </div>
        ))}

        {hasMore && (
          <div className="text-center">
            <button
              onClick={loadMoreEmails}
              disabled={isLoadingMore}
              className="px-4 py-2 text-black bg-gray-400 opacity-50 hover:bg-blue-700 rounded"
            >
              {isLoadingMore ? "Đang tải..." : "Tải thêm"}
            </button>
          </div>
        )}
      </div>

      {selectedEmail && (
        <EmailDetailModal
          email={{ ...selectedEmail, content: selectedEmail?.content || "" }}
          onClose={() => setSelectedEmail(null)}
          markAsUnread={(id: string) => {
            setEmails((prevEmails) =>
              prevEmails.map((email) =>
                email.id === id ? { ...email, isRead: false } : email
              )
            );
          }}
        />
      )}

      {isEmailDetailOpen && (
        <ComposeEmailModal
          onClose={() => setIsEmailDetailOpen(false)}
          onCompose={() => console.log("Compose email")}
        />
      )}
    </div>
  );
};

export default Page;
