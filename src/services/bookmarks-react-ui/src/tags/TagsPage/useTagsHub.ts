import { useEffect } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import type { Tag } from "./models";

export function useTagsHub(onTagUpdated?: (tag: Tag) => void): void {
  useEffect(() => {
    const hubConnection = new HubConnectionBuilder()
      .withUrl("/api/tags/events")
      .withAutomaticReconnect()
      .build();

    hubConnection.on("OnTagUpdated", (tag: Tag) => {
      if (onTagUpdated) {
        onTagUpdated(tag);
      }
    });

    hubConnection
      .start()
      .then(() => {
        console.log("Connected to TagsHub");
      })
      .catch((err) => {
        console.error("Connection to TagsHub failed: ", err);
      });

    return () => {
      hubConnection.stop().catch((err) => {
        console.error("Error stopping TagsHub connection: ", err);
      });
    };
  }, [onTagUpdated]);
}
