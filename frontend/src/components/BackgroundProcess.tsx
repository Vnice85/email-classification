"use client"; // Chỉ file này cần "use client"

import { useEffect } from "react";
import { autoRefreshToken } from "@/utils/auth";

export default function BackgroundProcess() {
    useEffect(() => {
        const interval = setInterval(() => {
            autoRefreshToken();
        }, 30 * 60 * 1000); // 30 phút

        return () => clearInterval(interval);
    }, []);

    return null; // không cần render gì cả
}