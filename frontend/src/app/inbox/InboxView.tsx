"use client";

type Props = {
  onClose: () => void;
};
import { useState } from "react";
import { Sidebar } from "@/components/Sidebar";
import ComposeEmailModal from "@/components/ComposeEmailModal";
import EmailDetailModal from "@/components/EmailDetailModal";

type Email = {
  id: string;
  sender: string;
  subject: string;
  snippet: string;
  date: string;
  isRead: boolean;
  isStarred: boolean;
  labels: string[];
  content: string;
};

const mockEmails: Email[] = Array.from({ length: 10 }).map((_, i) => ({
  id: `email-${i}`,
  sender: `Người gửi ${i}`,
  subject: `Tiêu đề email ${i}`,
  snippet: `Nội dung mô tả nhanh của email ${i}`,
  date: "2025-06-13",
  isRead: Math.random() > 0.5,
  isStarred: Math.random() > 0.5, // Added isStarred property
  labels: [],
  content: `Nội dung chi tiết của email ${i}`,
}));

export default function InboxView() {
  const [emails, setEmails] = useState<Email[]>(mockEmails);
  const [selectedEmail, setSelectedEmail] = useState<Email | null>(null);
  const [isComposeOpen, setIsComposeOpen] = useState(false);

  const handleOpenEmail = (email: Email) => {
    // TODO: Sau này nếu cần gọi API để lấy chi tiết email, thực hiện tại đây rồi setSelectedEmail
    setSelectedEmail(email);
    setEmails((prev) =>
      prev.map((e) => (e.id === email.id ? { ...e, isRead: true } : e))
    );
  };

  const handleAddLabel = (label: string, remove = false) => {
    if (!selectedEmail) return;
    setEmails((prev) =>
      prev.map((e) =>
        e.id === selectedEmail.id
          ? {
              ...e,
              labels: remove
                ? e.labels.filter((l) => l !== label)
                : Array.from(new Set([...e.labels, label])),
            }
          : e
      )
    );
    setSelectedEmail((prev) =>
      prev
        ? {
            ...prev,
            labels: remove
              ? prev.labels.filter((l) => l !== label)
              : Array.from(new Set([...prev.labels, label])),
          }
        : null
    );
  };

  const handleCompose = () => {
    setIsComposeOpen(true);
  };

  const handleCloseCompose = () => {
    setIsComposeOpen(false);
  };
  console.log(selectedEmail);
  return (
    <div className="flex flex-col md:flex-row min-h-screen">
      <Sidebar
        onCompose={handleCompose}
        setSearchContent={() => {}}
        setIsClassify={() => {}}
      />
      <div className="flex-1 px-4 sm:px-6 py-4 bg-gradient-to-br from-blue-200/40 via-white/20 to-purple-200/30 backdrop-blur-xl border border-white/30 ring-1 ring-white/20 rounded-xl shadow-2xl hover:shadow-blue-300/50 hover:scale-[1.01] transition-all duration-300 mx-2 sm:mx-4 my-4 sm:my-6">
        <h1 className="text-xl font-bold mb-6 ml-10 md:ml-0">Inbox</h1>
        <div className="divide-y divide-gray-200 overflow-x-auto">
          {emails.map((email) => (
            <div
              key={email.id}
              className="grid grid-cols-1 sm:grid-cols-[auto_auto_1fr_auto_auto] items-start sm:items-center gap-4 py-2 hover:bg-gray-50 border-b border-gray-200 px-2 sm:px-4"
            >
              <input type="checkbox" className="ml-2" />
              {!email.isRead && (
                <span className="h-2 w-2 bg-blue-500 rounded-full" />
              )}
              <div
                className="flex flex-col overflow-hidden cursor-pointer"
                onClick={() => handleOpenEmail(email)}
              >
                <div className="flex flex-wrap md:flex-nowrap gap-4 items-center truncate">
                  <span className="font-medium w-32 truncate">
                    {email.sender}
                  </span>
                  <span className="w-48 truncate">{email.subject}</span>
                  <span className="text-gray-500 truncate flex-1">
                    {email.snippet}
                  </span>
                </div>
                <div className="flex gap-1 text-xs text-gray-600 mt-1">
                  {email.labels.map((label) => (
                    <span
                      key={`${email.id}-${label}`}
                      className="bg-gray-100 px-2 py-0.5 rounded"
                    >
                      {label}
                    </span>
                  ))}
                </div>
              </div>
              <span className="text-sm text-gray-400 whitespace-nowrap">
                {email.date}
              </span>
              {email.isStarred ? (
                <span className="text-yellow-500">★</span>
              ) : (
                <span className="w-4" />
              )}
            </div>
          ))}
        </div>
        {selectedEmail && (
          <EmailDetailModal
            email={selectedEmail}
            onClose={() => setSelectedEmail(null)}
            markAsUnread={(id: string) =>
              setEmails((prev) =>
                prev.map((e) => (e.id === id ? { ...e, isRead: false } : e))
              )
            }
            onAddLabel={handleAddLabel}
          />
        )}
        {isComposeOpen && (
          <ComposeEmailModal
            onClose={handleCloseCompose}
            onCompose={handleCompose}
          />
        )}
      </div>
    </div>
  );
}
