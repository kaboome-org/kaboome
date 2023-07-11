import { setActivePinia, createPinia } from "pinia";
import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";
import { flushPromises } from "@vue/test-utils";
import { backendStore } from "src/stores/backend";
function createFetchResponse(data) {
  return { text: () => new Promise((resolve) => resolve(data)) };
}
describe("backendStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    global.fetch = vi.fn();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });
  it("should update lastBackendSyncStarted and lastBackendSyncEnded", async () => {
    const store = backendStore();
    const originalLastBackendSyncStarted = store.lastBackendSyncStarted;
    const originalLastBackendSyncEnded = store.lastBackendSyncEnded;
    fetch.mockResolvedValue(createFetchResponse('"OK"'));
    store.syncNow();

    expect(store.lastBackendSyncStarted).not.toBe(
      originalLastBackendSyncStarted
    );
    await flushPromises();
    expect(store.lastBackendSyncEnded).not.toBe(originalLastBackendSyncEnded);
  });
  it("should log a warning if the response is not 'OK'", async () => {
    const store = backendStore();
    const originalLastBackendSyncEnded = store.lastBackendSyncEnded;
    fetch.mockResolvedValue(createFetchResponse('"Error"'));

    store.syncNow();
    await flushPromises();
    expect(store.lastBackendSyncEnded).toBe(originalLastBackendSyncEnded);
  });
  it("should schedule the fetch call if there was a recent other call", async () => {
    const store = backendStore();
    fetch.mockResolvedValue(createFetchResponse('"OK"'));
    global.setTimeout = vi.fn();
    setTimeout.mockImplementation((fn, _) => fn());
    store.syncNow();
    const lastBackendSyncStartedAfterFirstCall = store.lastBackendSyncStarted;
    store.syncNow();
    expect(lastBackendSyncStartedAfterFirstCall).toBe(
      store.lastBackendSyncStarted
    );
    await flushPromises();
  });
  it("should reallow the fetch call if it didn't finish successfully for a long time", async () => {
    const store = backendStore();
    const originalLastBackendSyncStarted = store.lastBackendSyncStarted;
    store.lastBackendSyncEnded = 100;
    fetch.mockResolvedValue(createFetchResponse('"OK"'));
    store.syncNow();
    expect(store.lastBackendSyncEnded).not.toBe(100);
    expect(store.lastBackendSyncStarted).not.toBe(
      originalLastBackendSyncStarted
    );
  });
});
