const dateFormatter = new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
  year: "numeric",
  month: "long",
  day: "numeric",
});
const timeFormatter = new Intl.DateTimeFormat("fa-IR", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
});

export function formatPersianDateTime(value: Date) {
  return {
    date: dateFormatter.format(value),
    time: timeFormatter.format(value),
  };
}
