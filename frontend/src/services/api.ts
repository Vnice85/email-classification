import axios from "axios";

// Utility functions for guest session management
export const getGuestId = (): string | null => {
  return localStorage.getItem("guestId");
};

export const setGuestId = (guestId: string): void => {
  localStorage.setItem("guestId", guestId);
};

export const clearGuestId = (): void => {
  localStorage.removeItem("guestId");
};

export const isGuestMode = (): boolean => {
  return !!getGuestId() && !localStorage.getItem("jwtToken");
};

// Guest API functions
export const createGuestSession = async () => {
  const res = await axios.get("https://localhost:44366/guest/guestid");
  const { guestId } = res.data;
  setGuestId(guestId);
  return guestId;
};

export const getGuestEmails = async (params: {
  pageindex?: number;
  pagesize?: number;
  labelname?: string;
}) => {
  const guestId = localStorage.getItem("guestId");
  if (!guestId) throw new Error("No guest session found");

  const { pageindex = 1, pagesize = 20, labelname = "" } = params;

  let url = `https://localhost:44366/guest/messages?pageindex=${pageindex}&pagesize=${pagesize}`;
  if (labelname) {
    url += `&labelname=${labelname}`;
  }

  const res = await axios.get(url, {
    headers: {
      guestId: guestId,
    },
  });

  return res.data;
};

export const getGuestEmailDetail = async (emailId: string) => {
  const guestId = getGuestId();
  if (!guestId) throw new Error("No guest session found");

  const res = await axios.get(
    `https://localhost:44366/guest/messages/${emailId}`,
    {
      headers: { guestId },
    }
  );
  return res.data;
};

export const createGuestEmail = async (payload: {
  from: string;
  to: string;
  subject: string;
  body: string;
}) => {
  const guestId = getGuestId();
  if (!guestId) throw new Error("No guest session found");

  const res = await axios.post(
    "https://localhost:44366/guest/messages",
    payload,
    {
      headers: { guestId },
    }
  );
  return res.data;
};

export const deleteGuestEmail = async (emailId: string) => {
  const guestId = getGuestId();
  if (!guestId) throw new Error("No guest session found");

  const res = await axios.delete(
    `https://localhost:44366/guest/messages/${emailId}`,
    {
      headers: { guestId },
    }
  );
  return res.status === 204;
};

// Auth
export const refreshToken = async () => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.post(
    "https://localhost:44366/auth/refreshtoken",
    null,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

// Authenticated Email API
export const getEmailDetail = async (emailId: string) => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.get(
    `https://localhost:44366/email/messages/${emailId}`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  console.log(res.data);
  return res.data;
};

export const classifyEmails = async () => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.post(`https://localhost:44366/email/classify`, null, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

export const sendEmail = async (payload: {
  toAddress: string;
  subject: string;
  body: string;
}) => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.post(
    "https://localhost:44366/email/messages/send",
    payload,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

export const createDraftEmail = async (payload: {
  toAddress: string;
  subject: string;
  body: string;
}) => {
  const token = localStorage.getItem("jwtToken");
  console.log(token);

  const res = await axios.post(
    "https://localhost:44366/email/drafts/save",
    payload,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

export const deleteEmail = async (id: string) => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.delete(
    `https://localhost:44366/email/messages/${id}`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

export const searchEmails = async (
  pageindex: number,
  pagesize: number,
  keyword: string
) => {
  pageindex = pageindex || 1;
  pagesize = pagesize || 20;
  const token = localStorage.getItem("jwtToken");
  const res = await axios.get(
    `https://localhost:44366/email/messages/search?pageindex=${pageindex}&pagesize=${pagesize}&keyword=${encodeURIComponent(
      keyword
    )}`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

export const syncEmails = async () => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.post("https://localhost:44366/email/sync", null, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

export const getEmails = async (params: {
  pageindex?: number;
  pagesize?: number;
  labelname?: string;
  directionname?: string;
}) => {
  const token = localStorage.getItem("jwtToken");
  const {
    pageindex = 1,
    pagesize = 20,
    labelname = "",
    directionname = "INBOX",
  } = params;
  const res = await axios.get(
    `https://localhost:44366/email/messages?pageindex=${pageindex}&pagesize=${pagesize}&labelname=${labelname}&directionname=${directionname}`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

export const updateDraftEmail = async (
  id: string,
  payload: {
    toAddress: string;
    subject: string;
    body: string;
  }
) => {
  const token = localStorage.getItem("jwtToken");
  const res = await axios.put(
    `https://localhost:44366/email/drafts/${id}`,
    payload,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );
  return res.data;
};

export const EMAIL_API = {
  getEmailDetail,
  classifyEmails,
  sendEmail,
  createDraftEmail,
  deleteEmail,
  searchEmails,
  syncEmails,
  getEmails,
  // Guest APIs
  createGuestSession,
  getGuestEmails,
  getGuestEmailDetail,
  createGuestEmail,
  deleteGuestEmail,
  // Unified APIs
};
