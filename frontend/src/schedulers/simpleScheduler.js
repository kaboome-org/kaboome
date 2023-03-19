export function schedule(tasksToReschedule, blockers, calendar) {
  const minuteRounder = (t) => {
    const fiveMinutes = 5 * 60 * 1000;
    return t - (t % fiveMinutes) + fiveMinutes;
  };
  let potentialStart = minuteRounder(new Date());
  blockers.sort((a, b) => a.start - b.start);
  let blockerindex = 0;
  tasksToReschedule.forEach((task) => {
    const taskDuration = task.end - task.start;
    while (
      blockerindex < blockers.length &&
      (blockers[blockerindex].end < potentialStart ||
        blockers[blockerindex].start - potentialStart < taskDuration)
    ) {
      if (
        blockerindex < blockers.length &&
        ((blockers[blockerindex].end >= potentialStart &&
          blockers[blockerindex].start <= potentialStart) ||
          (blockers[blockerindex].end >= potentialStart + taskDuration &&
            blockers[blockerindex].start <= potentialStart + taskDuration))
      ) {
        potentialStart = minuteRounder(blockers[blockerindex].end);
      }
      blockerindex++;
    }
    task.start = potentialStart;
    task.end = potentialStart + taskDuration;
    potentialStart = minuteRounder(task.end);
    calendar.put(task);
  });
}
