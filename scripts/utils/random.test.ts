import { Random } from "./random.ts";
import { assertEquals } from "jsr:@std/assert";

Deno.test("getRandomColor should return a color but the same for the same seed", () => {
    const random1 = new Random("test");
    const color1 = random1.getRandomColor();

    const random2 = new Random("test");
    const color2 = random2.getRandomColor();

    assertEquals(color1, color2);
});
