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

describe("addOrMergeWriteOnlyEvents method tests", () => {
  it("should add a WO-event if its kaboome source is configured, but its not yet part of the fullcalendar event", () => {
    const row = {
      doc: {
        _id: "kaboome-1234",
      },
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
    const fullcalendarEvent = {
      extendedProps: {
        WriteOnlyExternalEvents: [],
      },
    };
    const ret = addOrMergeWriteOnlyEvents(
      row,
      configuration,
      fullcalendarEvent
    );
    expect(ret).toBe(true);
    expect(fullcalendarEvent.extendedProps.WriteOnlyExternalEvents.length).toBe(
      1
    );
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0].SyncType
    ).toBe(2);
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleAccountId
    ).toBe("test@gmail.com");
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleCalendarId
    ).toBe("test-calendar@gmail.com");
  });
  it("should return false if configuration requires no event, and the fullcalendar event doesn't have any as well", () => {
    const row = {
      doc: {
        _id: "google-1234",
        ReadWriteExternalEvent: { GoogleCalendarPath: {} },
      },
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
    const fullcalendarEvent = {
      extendedProps: {
        WriteOnlyExternalEvents: [],
      },
    };
    const ret = addOrMergeWriteOnlyEvents(
      row,
      configuration,
      fullcalendarEvent
    );
    expect(ret).toBe(false);
    expect(fullcalendarEvent.extendedProps.WriteOnlyExternalEvents.length).toBe(
      0
    );
  });
  it(`should change WO-event if its kaboome source is configured, but its fullcalendar
   event doesn't match the configuration`, () => {
    const row = {
      doc: {
        _id: "kaboome-1234",
      },
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
    const fullcalendarEvent = {
      extendedProps: {
        WriteOnlyExternalEvents: [
          {
            GoogleCalendarPath: {
              GoogleAccountId: "test@gmail.com",
              GoogleCalendarId: "test-calendar@gmail.com",
            },
            ManuallyEdited: false,
            SyncType: 1,
          },
        ],
      },
    };
    const ret = addOrMergeWriteOnlyEvents(
      row,
      configuration,
      fullcalendarEvent
    );
    expect(ret).toBe(true);
    expect(fullcalendarEvent.extendedProps.WriteOnlyExternalEvents.length).toBe(
      1
    );
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0].SyncType
    ).toBe(2);
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleAccountId
    ).toBe("test@gmail.com");
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleCalendarId
    ).toBe("test-calendar@gmail.com");
  });
  it(`shouldn't change WO-event if its manually edited`, () => {
    const row = {
      doc: {
        _id: "kaboome-1234",
      },
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
    const fullcalendarEvent = {
      extendedProps: {
        WriteOnlyExternalEvents: [
          {
            GoogleCalendarPath: {
              GoogleAccountId: "test@gmail.com",
              GoogleCalendarId: "test-calendar@gmail.com",
            },
            ManuallyEdited: true,
            SyncType: 1,
          },
        ],
      },
    };
    const ret = addOrMergeWriteOnlyEvents(
      row,
      configuration,
      fullcalendarEvent
    );
    expect(ret).toBe(false);
    expect(fullcalendarEvent.extendedProps.WriteOnlyExternalEvents.length).toBe(
      1
    );
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0].SyncType
    ).toBe(1);
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleAccountId
    ).toBe("test@gmail.com");
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleCalendarId
    ).toBe("test-calendar@gmail.com");
  });
  it(`should remove WO-event if its kaboome source is not configured, but its fullcalendar
   event exists`, () => {
    const row = {
      doc: {
        _id: "kaboome-1234",
      },
    };
    const configuration = [
      {
        label: "google-test@gmail.com",
        children: [
          {
            label: "test-calendar@gmail.com",
            SyncFromCalendars: [],
          },
        ],
      },
    ];
    const fullcalendarEvent = {
      extendedProps: {
        WriteOnlyExternalEvents: [
          {
            GoogleCalendarPath: {
              GoogleAccountId: "test@gmail.com",
              GoogleCalendarId: "test-calendar@gmail.com",
            },
            ManuallyEdited: true,
            SyncType: 1,
          },
        ],
      },
    };
    const ret = addOrMergeWriteOnlyEvents(
      row,
      configuration,
      fullcalendarEvent
    );
    expect(ret).toBe(true);
    expect(fullcalendarEvent.extendedProps.WriteOnlyExternalEvents.length).toBe(
      0
    );
  });
  it("should return false if kaboome source is configured and its already part of the fullcalendar event", () => {
    const row = {
      doc: {
        _id: "kaboome-1234",
      },
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
    const fullcalendarEvent = {
      extendedProps: {
        WriteOnlyExternalEvents: [
          {
            GoogleCalendarPath: {
              GoogleAccountId: "test@gmail.com",
              GoogleCalendarId: "test-calendar@gmail.com",
            },
            SyncType: 2,
            ManuallyEdited: false,
          },
        ],
      },
    };
    const ret = addOrMergeWriteOnlyEvents(
      row,
      configuration,
      fullcalendarEvent
    );
    expect(ret).toBe(false);
    expect(fullcalendarEvent.extendedProps.WriteOnlyExternalEvents.length).toBe(
      1
    );
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0].SyncType
    ).toBe(2);
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleAccountId
    ).toBe("test@gmail.com");
    expect(
      fullcalendarEvent.extendedProps.WriteOnlyExternalEvents[0]
        .GoogleCalendarPath.GoogleCalendarId
    ).toBe("test-calendar@gmail.com");
  });
});
