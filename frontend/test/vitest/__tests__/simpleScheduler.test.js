import { describe, expect, it } from "vitest";
import { schedule } from "src/schedulers/simpleScheduler";

describe("schedule method tests", () => {
  it("should schedule tasks and keep duration", () => {
    const tasks = [
      {
        start: new Date(2023, 3, 1, 9, 0),
        end: new Date(2023, 3, 1, 10, 0),
        extendedProps: { isTask: true, isDone: false },
        commited: false,
      },
    ];
    const duration = tasks[0].end - tasks[0].start;
    schedule(tasks, [], { put: (task) => (task.commited = true) });
    expect(tasks[0].end - tasks[0].start).toBe(duration);
    expect(tasks[0].commited).toBe(true);
    expect(tasks[0].start - new Date()).toBeGreaterThanOrEqual(0);
  });
  it("should respect blockers", () => {
    const tasks = [
      {
        id: 1,
        start: new Date(2023, 3, 1, 9, 0),
        end: new Date(2023, 3, 1, 10, 0),
        extendedProps: { isTask: true, isDone: false },
      },
    ];
    const blockers = [
      {
        id: 1,
        start: new Date(),
        end: new Date().valueOf() + 60000,
        extendedProps: { isTask: true, isDone: false },
      },
    ];
    schedule(tasks, blockers, { put: (_) => {} });
    expect(tasks[0].start - new Date()).toBeGreaterThanOrEqual(60000);
  });
});
