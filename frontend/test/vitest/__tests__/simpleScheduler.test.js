import { describe, expect, it } from "vitest";
import { schedule } from "src/schedulers/simpleScheduler";
const MINUTE_MILLIS = 60 * 1000;
describe("schedule method tests", () => {
  it("should schedule tasks and keep duration", () => {
    const tasks = [
      {
        start: new Date(2023, 3, 1, 9, 0),
        end: new Date(2023, 3, 1, 10, 0),
        commited: false,
      },
    ];
    const duration = tasks[0].end - tasks[0].start;
    schedule(tasks, [], { put: (task) => (task.commited = true) });
    expect(tasks[0].end - tasks[0].start).toBe(duration);
    expect(tasks[0].commited).toBe(true);
    expect(tasks[0].start - new Date()).toBeGreaterThanOrEqual(0);
    expect(tasks[0].start - new Date()).toBeLessThanOrEqual(10 * MINUTE_MILLIS);
  });
  it("should respect blockers", () => {
    const tasks = [
      {
        id: 1,
        start: new Date(2023, 3, 1, 9, 0),
        end: new Date(2023, 3, 1, 10, 0),
      },
    ];
    const blockers = [
      {
        id: 1,
        start: new Date(),
        end: new Date().valueOf() + 60 * MINUTE_MILLIS,
      },
    ];
    schedule(tasks, blockers, { put: (_) => {} });
    expect(tasks[0].start - new Date()).toBeGreaterThanOrEqual(
      60 * MINUTE_MILLIS
    );
    expect(tasks[0].start - new Date()).toBeLessThanOrEqual(70 * MINUTE_MILLIS);
  });
  it("should respect blockers", () => {
    const tasks = [
      {
        id: 1,
        start: new Date(2023, 3, 1, 9, 0),
        end: new Date(2023, 3, 1, 10, 0),
      },
    ];
    const blockers = [
      {
        id: 1,
        start: new Date().valueOf() + 30 * MINUTE_MILLIS,
        end: new Date().valueOf() + 90 * MINUTE_MILLIS,
      },
    ];
    schedule(tasks, blockers, { put: (_) => {} });
    expect(tasks[0].start - new Date()).toBeLessThanOrEqual(
      100 * MINUTE_MILLIS
    );
    expect(tasks[0].start - new Date()).toBeGreaterThanOrEqual(
      90 * MINUTE_MILLIS
    );
  });
  it("should respect blockers", () => {
    const tasks = [
      {
        id: 1,
        start: new Date(2023, 3, 1, 9, 0),
        end: new Date(2023, 3, 1, 10, 0),
      },
    ];
    const blockers = [
      {
        id: 1,
        start: new Date().valueOf() + 70 * MINUTE_MILLIS,
        end: new Date().valueOf() + 90 * MINUTE_MILLIS,
      },
    ];
    schedule(tasks, blockers, { put: (_) => {} });
    expect(tasks[0].start - new Date()).toBeGreaterThanOrEqual(0);
    expect(tasks[0].start - new Date()).toBeLessThanOrEqual(10 * MINUTE_MILLIS);
  });
});
