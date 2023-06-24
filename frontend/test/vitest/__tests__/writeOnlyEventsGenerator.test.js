import { describe, expect, it } from "vitest";
import {
  writeOnlyEvents,
  addOrMergeWriteOnlyEvents,
} from "src/helpers/writeOnlyEventsGenerator";

describe("writeOnlyEvents method tests", () => {
  it("should return a WO-event if its kaboome source is configured", () => {
    const source = {
      Vendor: "kaboome",
      VendorCalendarPathJson: "null",
    };
    const configuration = [
      {
        label: "google-test@gmail.com",
        children: [
          {
            label: "test-calendar@gmail.com",
            SyncFromCalendars: [
              {
                Vendor: "kaboome",
                VendorCalendarPathJson: "null",
                SyncType: 2,
              },
            ],
          },
        ],
      },
    ];
    const ret = writeOnlyEvents(source, configuration);
    expect(ret.length).toBe(1);
    expect(ret[0].ManuallyEdited).toBe(false);
    expect(ret[0].SyncType).toBe(2);
    expect(ret[0].GoogleCalendarPath.GoogleAccountId).toBe("test@gmail.com");
    expect(ret[0].GoogleCalendarPath.GoogleCalendarId).toBe(
      "test-calendar@gmail.com"
    );
  });
  it("shouldn't return any WO-event if its kaboome source is not configured", () => {
    const source = {
      Vendor: "kaboome",
      VendorCalendarPathJson: "null",
    };
    const configuration = [
      {
        label: "google-test@gmail.com",
        children: [
          {
            label: "test-calendar@gmail.com",
            SyncFromCalendars: [
              {
                Vendor: "google",
                VendorCalendarPathJson:
                  '{"GoogleAccountId": "test2@gmail.com", "GoogleCalendarId":"test2-calendar@gmail.com"}',
                SyncType: 1,
              },
            ],
          },
        ],
      },
    ];
    const ret = writeOnlyEvents(source, configuration);
    expect(ret.length).toBe(0);
  });
  it("should return a WO-event if its google source is configured", () => {
    const source = {
      Vendor: "google",
      VendorCalendarPathJson:
        '{"GoogleAccountId": "test2@gmail.com", "GoogleCalendarId":"test2-calendar@gmail.com"}',
    };
    const configuration = [
      {
        label: "google-test@gmail.com",
        children: [
          {
            label: "test-calendar@gmail.com",
            SyncFromCalendars: [
              {
                Vendor: "google",
                VendorCalendarPathJson:
                  '{"GoogleAccountId": "test2@gmail.com", "GoogleCalendarId":"test2-calendar@gmail.com"}',
                SyncType: 1,
              },
            ],
          },
        ],
      },
    ];
    const ret = writeOnlyEvents(source, configuration);
    expect(ret.length).toBe(1);
    expect(ret[0].ManuallyEdited).toBe(false);
    expect(ret[0].SyncType).toBe(1);
    expect(ret[0].GoogleCalendarPath.GoogleAccountId).toBe("test@gmail.com");
    expect(ret[0].GoogleCalendarPath.GoogleCalendarId).toBe(
      "test-calendar@gmail.com"
    );
  });
  it("shouldn't return any WO-event if its google source is not configured", () => {
    const source = {
      Vendor: "google",
      VendorCalendarPathJson:
        '{"GoogleAccountId": "test@gmail.com", "GoogleCalendarId":"test-calendar@gmail.com"}',
    };
    const configuration = [
      {
        label: "google-test@gmail.com",
        children: [
          {
            label: "test-calendar@gmail.com",
            SyncFromCalendars: [
              {
                Vendor: "google",
                VendorCalendarPathJson:
                  '{"GoogleAccountId": "test2@gmail.com", "GoogleCalendarId":"test2-calendar@gmail.com"}',
                SyncType: 1,
              },
            ],
          },
        ],
      },
    ];
    const ret = writeOnlyEvents(source, configuration);
    expect(ret.length).toBe(0);
  });
});
