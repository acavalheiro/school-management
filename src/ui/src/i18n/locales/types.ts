/** The shape of a catalogue with every leaf widened to `string`, so other languages must match en-GB key-for-key. */
export type Messages<T> = {
  [K in keyof T]: T[K] extends string ? string : Messages<T[K]>;
};
